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
    hhg


hhhhhhh

Option Explicit

'============================================================
' FLABSO – Performance Attribution (Buckets) – BAR + CUM
'
' Source workbook: wb_correction
' Source sheet   : "INT_FLABSO"
'
' Expected source layout:
'   - Row 1 contains headers
'   - Column A (col 1) contains bucket labels (Interest, Capital, Cash, Mgt Fees, Perf Fees, Other)
'   - Date columns start at column F (col 6), each header is a date (as text or Excel date)
'   - Values are DAILY contributions in % (as decimals, e.g. 0.0003 = 3 bps, or 0.003 = 0.3%)
'
' Output:
'   - In wb_final, creates/overwrites:
'       * "FLABSO_BAR" : daily contributions by bucket
'       * "FLABSO_CUM" : cumulative compounded contribution by bucket over the quarter
'
' Notes:
'   - CUM logic: builds an index per bucket: I_t = I_{t-1}*(1+r_t), starting at 1
'     and then stores (I_t - 1) as cumulative contribution
'============================================================

Public Sub FLABSO_ATTRIB_BAR_CUM(ByVal wb_correction As Workbook, ByVal wb_final As Workbook)
    Dim ws_src As Worksheet
    Set ws_src = wb_correction.Sheets("INT_FLABSO")

    ' Build BAR
    Dim ws_bar As Worksheet
    Set ws_bar = GetOrCreateSheet(wb_final, "FLABSO_BAR")
    ws_bar.Cells.Clear
    BuildPivotToSheet ws_src, ws_bar, "Bucket", 6

    ' Format BAR
    FormatAttribSheet ws_bar, "Daily bucket contribution (BAR)", True

    ' Build CUM (copy BAR then transform)
    Dim ws_cum As Worksheet
    Set ws_cum = GetOrCreateSheet(wb_final, "FLABSO_CUM")
    ws_cum.Cells.Clear
    ws_bar.UsedRange.Copy
    ws_cum.Range("A1").PasteSpecial Paste:=xlPasteValues
    Application.CutCopyMode = False

    TransformToCumulative ws_cum

    ' Format CUM
    FormatAttribSheet ws_cum, "Cumulative bucket contribution (CUM)", True

End Sub


'========================
' Core: Build pivot result
'========================
Private Sub BuildPivotToSheet(ByVal ws_src As Worksheet, ByVal ws_out As Worksheet, _
                             ByVal bucketFieldName As String, ByVal firstDateCol As Long)

    Dim lastRow As Long, lastCol As Long
    lastRow = ws_src.Cells(ws_src.Rows.Count, 1).End(xlUp).Row
    lastCol = ws_src.Cells(1, ws_src.Columns.Count).End(xlToLeft).Column

    If lastRow < 2 Or lastCol < firstDateCol Then
        Err.Raise vbObjectError + 100, "BuildPivotToSheet", "Source sheet doesn't look like expected (not enough rows/cols)."
    End If

    ' Ensure bucket header exists in col A (Pivot needs a field name)
    If Trim$(CStr(ws_src.Cells(1, 1).Value)) = "" Then
        ws_src.Cells(1, 1).Value = bucketFieldName
    End If

    Dim rngData As Range
    Set rngData = ws_src.Range(ws_src.Cells(1, 1), ws_src.Cells(lastRow, lastCol))

    ' Create PivotCache & PivotTable
    Dim pvtCache As PivotCache
    Dim pvtTable As PivotTable

    ' Clean any existing pivots on output sheet
    On Error Resume Next
    ws_out.PivotTables(1).TableRange2.Clear
    On Error GoTo 0

    Set pvtCache = ws_out.Parent.PivotCaches.Create(SourceType:=xlDatabase, SourceData:=rngData)

    Set pvtTable = ws_out.PivotTables.Add( _
                    PivotCache:=pvtCache, _
                    TableDestination:=ws_out.Range("A1"), _
                    TableName:="PVT_FLABSO_ATTRIB")

    ' Row field = Bucket (col A)
    With pvtTable.PivotFields(bucketFieldName)
        .Orientation = xlRowField
        .Position = 1
    End With

    ' Add each date column as a DataField (Sum)
    Dim c As Long
    Dim header As String
    Dim df As PivotField

    For c = firstDateCol To lastCol
        header = CStr(ws_src.Cells(1, c).Text)
        If Len(Trim$(header)) > 0 Then
            On Error Resume Next
            Set df = pvtTable.PivotFields(header)
            On Error GoTo 0

            If Not df Is Nothing Then
                df.Orientation = xlDataField
                df.Function = xlSum
                df.NumberFormat = "0.00%"  ' daily % contribution
            End If

            Set df = Nothing
        End If
    Next c

    ' Make it readable
    pvtTable.RowAxisLayout xlTabularRow
    pvtTable.RepeatAllLabels xlRepeatLabels

    ' Copy pivot values to a clean table (starting A1)
    pvtTable.TableRange1.Copy
    ws_out.Range("A1").PasteSpecial Paste:=xlPasteValues
    Application.CutCopyMode = False

    ' Clear pivot object (optional; keeps sheet light)
    On Error Resume Next
    pvtTable.TableRange2.Clear
    On Error GoTo 0

    ws_out.Cells.EntireColumn.AutoFit
End Sub


'========================
' Transform: BAR -> CUM
'========================
Private Sub TransformToCumulative(ByVal ws As Worksheet)
    Dim lastRow As Long, lastCol As Long
    lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
    lastCol = ws.Cells(1, ws.Columns.Count).End(xlToLeft).Column

    If lastRow < 2 Or lastCol < 2 Then Exit Sub

    ' Expect layout:
    '   Row 1 = headers (Bucket, Date1, Date2, ...)
    '   Col 1 = bucket names
    '   Data block = B2:lastCol,lastRow

    Dim r As Long, c As Long
    Dim idx As Double, daily As Double

    For r = 2 To lastRow
        idx = 1#
        For c = 2 To lastCol
            If IsNumeric(ws.Cells(r, c).Value) Then
                daily = CDbl(ws.Cells(r, c).Value)
                idx = idx * (1# + daily)
                ws.Cells(r, c).Value = idx - 1#   ' cumulative contribution
            Else
                ' leave as-is if blank/non-numeric
            End If
        Next c
    Next r

    ws.Range(ws.Cells(2, 2), ws.Cells(lastRow, lastCol)).NumberFormat = "0.00%"
End Sub


'========================
' Formatting helper
'========================
Private Sub FormatAttribSheet(ByVal ws As Worksheet, ByVal title As String, ByVal formatAsPercent As Boolean)
    With ws
        ' Title row insertion
        .Rows(1).Insert
        .Range("A1").Value = title
        .Range("A1").Font.Bold = True

        ' Header row (now row 2)
        .Rows(2).Font.Bold = True
        .Columns(1).Font.Bold = True

        ' Freeze panes below headers
        .Activate
        .Range("B3").Select
        ActiveWindow.FreezePanes = True

        If formatAsPercent Then
            ' Keep column 1 as text, data as %
            Dim lastRow As Long, lastCol As Long
            lastRow = .Cells(.Rows.Count, 1).End(xlUp).Row
            lastCol = .Cells(2, .Columns.Count).End(xlToLeft).Column
            If lastRow >= 3 And lastCol >= 2 Then
                .Range(.Cells(3, 2), .Cells(lastRow, lastCol)).NumberFormat = "0.00%"
            End If
        End If

        .Cells.EntireColumn.AutoFit
    End With
End Sub


'========================
' Sheet helper
'========================
Private Function GetOrCreateSheet(ByVal wb As Workbook, ByVal sheetName As String) As Worksheet
    On Error Resume Next
    Set GetOrCreateSheet = wb.Sheets(sheetName)
    On Error GoTo 0

    If GetOrCreateSheet Is Nothing Then
        Set GetOrCreateSheet = wb.Sheets.Add(After:=wb.Sheets(wb.Sheets.Count))
        GetOrCreateSheet.Name = sheetName
    End If
End Function

