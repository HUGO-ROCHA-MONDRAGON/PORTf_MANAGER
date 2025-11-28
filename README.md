# Option Pricing and Structured Strategies Engine (C# / .NET)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![WPF](https://img.shields.io/badge/UI-WPF-blueviolet)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()
[![Tests](https://img.shields.io/badge/Tests-xUnit-informational)]()

A complete option-pricing and strategy analysis engine developed in C# during the first semester of the second year of my Masters at Dauphine-PSL.

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

---

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

---

## User Modes

### Beginner Mode
- Guided interface for non-specialists
- Simple objectives: moderate rise, fall, volatility, stability
- Automatic selection of appropriate option strategies

### Expert Mode
- Full control over legs, strikes, maturities, and quantities
- Strategy recognition and detailed analytics
- Monte Carlo optional run

---

## Unit Tests
More than 50 tests ensure correctness and stability:

- Black–Scholes parity and monotonicity  
- Consistency between Monte Carlo and analytical pricing  
- Structure detection (spreads, straddles, condors, etc.)  
- Portfolio aggregation and payoff export  
- Basket normalization and correlation handling  

Run all tests:
```bash
dotnet test

