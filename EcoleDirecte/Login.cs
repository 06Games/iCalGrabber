namespace iCalGrabber.EcoleDirecte;

public class Login
{
    public string token { get; set; }
    public Data data { get; set; }
}
public class Data
{
    public List<Account> accounts { get; set; }
}
public class Account
{
    public string identifiant { get; set; }
    public string nomEtablissement { get; set; }
    public Profile profile { get; set; }
}

public class Profile
{
    public List<Eleve> eleves { get; set; }
}
public class Eleve
{
    public int id { get; set; }
    public string prenom { get; set; }
    public string nom { get; set; }
    public string nomEtablissement { get; set; }
}
