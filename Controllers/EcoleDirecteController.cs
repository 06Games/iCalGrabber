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

    private static Calendar Calendar;
    private static DateTime LastFetched = DateTime.MinValue;

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
        Logger.LogDebug(await result.Content.ReadAsStringAsync());
        var accountInfos = await JsonSerializer.DeserializeAsync<Login>(await result.Content.ReadAsStreamAsync());
        if (accountInfos == null)
            throw new NullReferenceException(await result.Content.ReadAsStringAsync());
        return (accountInfos.token, accountInfos.data.accounts.FirstOrDefault()?.profile.eleves ?? new List<Eleve>());
    }

    [HttpGet("ical")]
    public async Task<string> Get([Required] string user, [Required] string pass)
    {
        if (DateTime.Now - LastFetched >= TimeSpan.FromHours(24))
        {
            var (token, children) = await Connect(user, pass);
            Calendar = new Calendar();
            Calendar.AddProperty("X-WR-CALNAME", "Ecole Directe");

            foreach (var child in children)
                Calendar.Events.AddRange(await GetSchedule(child, token));
            LastFetched = DateTime.Now;
        }

        var serializer = new CalendarSerializer();
        var icalString = serializer.SerializeToString(Calendar);
        return icalString;
    }

    private async Task<IEnumerable<CalendarEvent>> GetSchedule(Eleve child, string token)
    {
        var param = new
        {
            token,
            dateDebut = DateTime.Now.ToString("yyyy-MM-dd"),
            dateFin = "2022-09-10",
            avecTrous = false
        };
        var result = await Client.PostAsync($"https://api.ecoledirecte.com/v3/E/{child.id}/emploidutemps.awp?verbe=get&", new StringContent($"data={JsonSerializer.Serialize(param)}"));
        Logger.LogDebug(await result.Content.ReadAsStringAsync());
        var json = await JsonSerializer.DeserializeAsync<EmploiDuTemps>(await result.Content.ReadAsStreamAsync());
        if (json == null)
            throw new NullReferenceException(await result.Content.ReadAsStringAsync());

        var events = json.data.Select(v => new CalendarEvent
        {
            Uid = v.id.ToString(),
            Summary = v.matiere,
            Organizer = new Organizer { CommonName = v.prof },
            DtStart = new CalDateTime(DateTime.Parse(v.start_date)),
            DtEnd = new CalDateTime(DateTime.Parse(v.end_date)),
            Location = v.salle,
            Status = v.isAnnule ? "CANCELLED" : null,
            Categories = new[] { child.id.ToString() },
            Attendees = new[] { new Attendee { CommonName = $"{child.prenom} {child.nom}" } }
        });
        return events;
    }
}
