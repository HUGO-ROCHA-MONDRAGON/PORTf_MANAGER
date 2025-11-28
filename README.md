# Option Pricing and Structured Strategies Engine (C# / .NET)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![WPF](https://img.shields.io/badge/UI-WPF-blueviolet)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()
[![Tests](https://img.shields.io/badge/Tests-xUnit-informational)]()

A complete option-pricing and strategy analysis engine developed in C# during the first semester of the **Master 272 – Financial Engineering** at **Université Paris Dauphine–PSL**.

This project integrates real-market data, a modular financial engine, a WPF graphical interface, Monte Carlo simulation, and more than 50 unit tests to ensure robustness.

---

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Financial Models](#financial-models)
- [User Modes](#user-modes)
- [Unit Tests](#unit-tests)
- [Limitations and Next Steps](#limitations-and-next-steps)
- [Technologies](#technologies)

---

## Overview
This project implements a full workflow for pricing European equity options and structured option strategies.  
It retrieves market data (spot, volatility, dividends, correlations) from the Twelve Data API and exposes two user paths:

- A guided **Beginner Mode**, tailored for non-specialists.
- A **Professional Mode**, enabling full custom strategy construction and analytics.

The goal was to design a compact, reusable, object-oriented financial engine similar to what is used on real trading or asset-management desks.

---

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

---

## Architecture
The solution is composed of four independent .NET projects:

