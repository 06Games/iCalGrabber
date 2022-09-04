namespace iCalGrabber.EcoleDirecte;

public class EmploiDuTemps
{
    public int code { get; set; }
    public string token { get; set; }
    public string host { get; set; }
    public List<Event> data { get; set; }
}

public class Event
{
    public int id { get; set; }
    public string text { get; set; }
    public string matiere { get; set; }
    public string codeMatiere { get; set; }
    public string typeCours { get; set; }
    public string start_date { get; set; }
    public string end_date { get; set; }
    public string color { get; set; }
    public bool dispensable { get; set; }
    public int dispense { get; set; }
    public string prof { get; set; }
    public string salle { get; set; }
    public string classe { get; set; }
    public int classeId { get; set; }
    public string classeCode { get; set; }
    public string groupe { get; set; }
    public string groupeCode { get; set; }
    public bool isFlexible { get; set; }
    public int groupeId { get; set; }
    public string icone { get; set; }
    public bool isModifie { get; set; }
    public bool contenuDeSeance { get; set; }
    public bool devoirAFaire { get; set; }
    public bool isAnnule { get; set; }
}
