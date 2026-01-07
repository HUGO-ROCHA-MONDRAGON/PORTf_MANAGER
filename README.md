# Option Pricing and Structured Strategies Engine (C# / .NET)

A complete option-pricing and strategy analysis engine developed in C# during the first semester of the second year of my Masters at Dauphine-PSL.

This project integrates real-market data, a modular financial engine, a WPF graphical interface, Monte Carlo simulation, and more than 50 unit tests to ensure robustness.

## Overview
This project implements a full workflow for pricing European equity options and structured option strategies.  
It retrieves market data (spot, volatility, dividends, correlations) from the Twelve Data API and exposes two user paths:

- A guided **Beginner Mode**, tailored for non-specialists.
- A **Professional Mode**, enabling full custom strategy construction and analytics.

The goal was to design a compact, reusable, object-oriented financial engine similar to what is used on real trading or asset-management desks.


## Features

### Market Data
- Daily historical prices (365 days) via the free Twelve Data API.
- Computed inputs: log-return volatility (annualized), dividend yield estimates, basket volatility and correlations.

### Pricing Capabilities
- **Black–Scholes closed-form model** with Greeks (Delta, Gamma, Vega, Theta, Rho).
- **Monte Carlo pricer** with log-normal dynamics and finite-difference Greeks.

### Supported Structures
- Vanilla European options (call/put, long/short).
- Bull call spread, bear put spread.
- Straddle, strangle.
- Butterfly.
- Calendar spread.
- Iron condor.
- Automatic structure pattern recognition.

### Output and Visualization
- Payoff diagrams (ScottPlot).
- Aggregated Greeks.
- CSV export.


## Architecture
The solution is composed of four independent .NET projects:

Pricing.Core – Financial engine (options, strategies, pricers, portfolio)
Pricing.CLI – Market data (Twelve Data API, basket builder)
Pricing.WPF – User interface (guided workflow, expert mode, charts)
Pricing.Tests – xUnit test suite (pricing, Greeks, baskets, structures)


### Object-Oriented Design
- Abstract classes: `Option`, `Strategy`
- Interfaces: `IPricingMethod`
- Concrete implementations: `CallOption`, `PutOption`, spreads, straddle…
- Portfolio aggregation for multi-leg strategies
- Swappable pricers via polymorphism


## Financial Models

### Black–Scholes Pricer
- Continuous dividend yield
- Greeks computed analytically
- Stability checks and bounded inputs

### Monte Carlo Pricer
- Log-normal paths  
- Discounted expected payoff  
- Greeks via bump-and-revalue  

### Basket Construction
- Weighted average of spot and dividends  
- Volatility computed from aggregated returns  
- Full correlation matrix included  


## User Modes

### Beginner Mode
- Guided interface for non-specialists
- Simple objectives: moderate rise, fall, volatility, stability
- Automatic selection of appropriate option strategies

### Expert Mode
- Full control over legs, strikes, maturities, and quantities
- Strategy recognition and detailed analytics
- Monte Carlo optional run


## Unit Tests
More than 50 tests ensure correctness and stability:

- Black–Scholes parity and monotonicity  
- Consistency between Monte Carlo and analytical pricing  
- Structure detection (spreads, straddles, condors, etc.)  
- Portfolio aggregation and payoff export  
- Basket normalization and correlation handling  


nav_date = ThisWorkbook.Worksheets("IS").Cells(2, 3).Value
other_perc = Range("other_perc").Value

If other_perc = 0 Then Exit Sub

row_other = Application.Match(other_perc, w_mon.Range("E:E"), 0)
If IsError(row_other) Then
    MsgBox "other_perc non trouvé dans la colonne E"
    Exit Sub
End If

If to_do <> 0 Then  ' avec la marge d'erreur de sigma (haut/bas)

    w_work.Name = "Working"
    Set w_dest = w_work.Range("A1")

    If Not dl Is Nothing Then
        On Error Resume Next
        Set rngfilt = dl.SpecialCells(xlCellTypeVisible)
        On Error GoTo 0

        If Not rngfilt Is Nothing Then
            rngfilt.Copy
            w_dest.PasteSpecial Paste:=xlPasteValues
            Application.CutCopyMode = False
        End If
    End If

    explained = 0
    i = 1
    k = 18

    If sum_other > 0 Then
        ' >>> Filtrer colonne K en décroissant (TON COMMENTAIRE) :
        SortByK w_work, True

        Do While explained < 1 And i <= w_work.Cells(w_work.Rows.Count, "L").End(xlUp).Row
            new_expl = w_work.Cells(i, 12).Value / other_perc
            explained = explained + new_expl

            w_mon.Cells(k, "I").Value = w_mon.Cells(8, "I").Value
            w_mon.Cells(k, "J").Value = w_mon.Cells(8, "J").Value
            w_mon.Cells(k, "K").Value = w_mon.Cells(8, "K").Value

            w_mon.Cells(k, "L").Value = w_work.Cells(i, 1).Value
            w_mon.Cells(k, "M").Value = w_work.Cells(i, 12).Value * nav_date
            w_mon.Cells(k, "N").Value = ThisWorkbook.Worksheets("IS").Cells(3, 3).Value
            w_mon.Cells(k, "O").Value = w_work.Cells(i, 12).Value
            w_mon.Cells(k, "P").Value = new_expl

            i = i + 1
            k = k + 1
        Loop

        w_mon.Cells(row_other, 5).Value = explained * other_perc

    Else
        ' >>> Filtrer colonne K en croissant (TON COMMENTAIRE) :
        SortByK w_work, False

        Do While explained < 1 And i <= w_work.Cells(w_work.Rows.Count, "L").End(xlUp).Row
            explained = explained + w_work.Cells(i, 12).Value / other_perc
            i = i + 1
        Loop

        w_mon.Cells(row_other, 5).Value = explained * other_perc
    End If

End If

