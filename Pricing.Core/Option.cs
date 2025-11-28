namespace Pricing.Core.Models;

/// Classe de base pour les options européennes (Call/Put)
/// Contient les paramètres classiques du modèle Black-Scholes
public abstract class Option
{
    // Prix spot du sous-jacent
    public double S0 { get; protected set; }
    
    // Strike (prix d'exercice)
    public double K  { get; protected set; }
    
    // Taux sans risque
    public double r  { get; protected set; }
    
    // Taux de dividende
    public double q  { get; protected set; }
    
    // Maturité en années
    public double T  { get; protected set; }
    
    // Volatilité: seul paramètre modifiable (utile pour calcul de vol implicite)
    public double Sigma { get; set; }

    protected Option(double s0, double k, double r, double q, double tYears, double sigma)
    {
        // Init rapide de tous les params
        S0 = s0; K = k; this.r = r; this.q = q; T = tYears; Sigma = sigma;
    }

    public abstract bool IsCall { get; }

    // Helper pour convertir une date en fraction d'année (convention 365j)
    public static double YearsFrom(DateTime today, DateTime maturity) =>
        Math.Max(0.0, (maturity - today).TotalDays / 365.0);
}

// Call européen classique
public sealed class CallOption : Option
{
    public CallOption(double s0, double k, double r, double q, double tYears, double sigma)
        : base(s0, k, r, q, tYears, sigma) {}
    public override bool IsCall => true;
}

// Put européen classique  
public sealed class PutOption : Option
{
    public PutOption(double s0, double k, double r, double q, double tYears, double sigma)
        : base(s0, k, r, q, tYears, sigma) {}
    public override bool IsCall => false;
}
