using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using iCalGrabber.EcoleDirecte;
using Microsoft.AspNetCore.Mvc;
namespace iCalGrabber.Controllers;

[ApiController]
[Route("/EcoleDirecte")]
public class EcoleDirecteController : ControllerBase
{
    private readonly HttpClient Client;
    private readonly ILogger<EcoleDirecteController> Logger;

    private static readonly Dictionary<string, CalendarValue> Database = new();


    public EcoleDirecteController(ILogger<EcoleDirecteController> logger)
    {
        Logger = logger;
        Client = new HttpClient();
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0");
    }

    private async Task<(string token, List<Eleve> children)> Connect(string user, string pass)
    {
        var param = new
        {
            uuid = "",
            identifiant = user,
            motdepasse = pass,
            isReLogin = false
        };
        var result = await Client.PostAsync("https://api.ecoledirecte.com/v3/login.awp", new StringContent($"data={JsonSerializer.Serialize(param)}"));
        var accountInfos = await JsonSerializer.DeserializeAsync<Login>(await result.Content.ReadAsStreamAsync());
        if (accountInfos == null)
            throw new NullReferenceException(await result.Content.ReadAsStringAsync());
        return (accountInfos.token, accountInfos.data.accounts.FirstOrDefault()?.profile.eleves ?? new List<Eleve>());
    }

    [HttpGet("ical")]
    public async Task<string> Get([Required] string user, [Required] string pass)
    {
        var refresh = false;

        using var sha1 = System.Security.Cryptography.SHA256.Create();
        var passSha = Convert.ToHexString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pass)));
        if (Database.TryGetValue(user, out var result))
        {
            if (DateTime.Now - result.LastFetched >= TimeSpan.FromHours(24))
                refresh = true;
            else if (passSha != result.PasswordSha)
                throw new ArgumentException("Password is incorrect (if you just changed it, wait 24h)");
        }
        else
            refresh = true;

        if (refresh)
        {
            var (token, children) = await Connect(user, pass);
            var calendar = new Calendar();
            calendar.AddTimeZone(new VTimeZone("Europe/Paris"));
            calendar.AddProperty("X-WR-CALNAME", $"Ecole Directe - {user}");

            foreach (var child in children)
                calendar.Events.AddRange(await GetSchedule(child, token));
            result = new CalendarValue { PasswordSha = passSha, Calendar = calendar, LastFetched = DateTime.Now };
            if (Database.ContainsKey(user)) Database[user] = result;
            else Database.Add(user, result);
        }

        var serializer = new CalendarSerializer();
        var icalString = serializer.SerializeToString(result.Calendar);
        return icalString;
    }

    private async Task<IEnumerable<CalendarEvent>> GetSchedule(Eleve child, string token)
    {
        var schoolYear = DateTime.Now.Month < 8 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
        var param = new
        {
            token,
            dateDebut = new DateTime(schoolYear, 8, 1).ToString("yyyy-MM-dd"),
            dateFin = new DateTime(schoolYear + 1, 7, 31).ToString("yyyy-MM-dd"),
            avecTrous = false
        };
        var result = await Client.PostAsync($"https://api.ecoledirecte.com/v3/E/{child.id}/emploidutemps.awp?verbe=get&", new StringContent($"data={JsonSerializer.Serialize(param)}"));
        var json = await JsonSerializer.DeserializeAsync<EmploiDuTemps>(await result.Content.ReadAsStreamAsync());
        if (json == null)
            throw new NullReferenceException(await result.Content.ReadAsStringAsync());

        var events = json.data.Select(v => new CalendarEvent
        {
            Uid = v.id.ToString(),
            Summary = v.matiere,
            Organizer = new Organizer { CommonName = v.prof },
            DtStart = new CalDateTime(DateTime.Parse(v.start_date), "Europe/Paris"),
            DtEnd = new CalDateTime(DateTime.Parse(v.end_date), "Europe/Paris"),
            Location = v.salle,
            Status = v.isAnnule ? "CANCELLED" : null,
            Categories = new[] { child.id.ToString() },
            Attendees = new[] { new Attendee { CommonName = $"{child.prenom} {child.nom}" } }
        });
        return events;
    }
}

internal class CalendarValue
{
    public string? PasswordSha;
    public Calendar? Calendar;
    public DateTime LastFetched = DateTime.MinValue;
}
