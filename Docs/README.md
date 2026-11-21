# Collector Documentation

Comprehensive documentation for the Collector platform — .NET 8 + React,
PostgreSQL, Ollama, headless-browser extraction, and an AI chat system that
researches topics and writes journal entries.

## Overview

- [Architecture](Architecture.md) — solution map, request flow, storage
  split, conventions

## Core platform

- [Web Server](Server.md) — ASP.NET host, middleware, SignalR hubs
- [API](Api.md) — controllers, ApiResponse conventions, services
- [Web Client](Client.md) — React app structure, api classes, conventions
- [Authentication](Authentication.md) — JWT, account flows, remote auth
- [Data Access](Data-Access.md) — Dapper repositories, entities, interfaces

## Content systems

- [Journal](Journal.md) — journals, categories, entries, modules, storage
  layout; per-module docs in [Journal/](Journal/)
- [Chat](Chat.md) — AI chat/research pipeline: plans, tools, journal
  generation; per-tool docs in [Chat/](Chat/)
- [Web Scraper](WebScraper.md) — rule-based web collection; per-section
  docs in [Web-Scraper/](Web-Scraper/)
- [Domains](Domains.md) — domain analysis & collection rules
- [Feeds](Feeds.md) — RSS/feed monitoring
- [Search Engines](Search-Engines.md) — engine registry + site-search
  discovery

## AI

- [Ollama](Ollama.md) — local model runtime: chat + HTML-parser models
- [Open AI](OpenAI.md) — OpenAI-compatible model registry + prompting
- [Speech](Speech.md) — TTS / STT helpers

## Infrastructure

- [Workers](Workers.md) — SignalR worker framework + background workers
- [Notifications](Notifications.md) — hub-by-hub pushes + email
- [Charlotte](Charlotte.md) — DOM extraction instances + Web Router +
  BlacklistParser
- [Chat CLI](Cli.md) — collector-chat commands & live testing
- [Chrome Extension](Chrome-Extension.md) — cookie relay for restricted
  downloads
- [Storing Content](Storing-Content.md) — filesystem layout + Azure
  convention
- [PostgreSQL Database](PostgreSQL.md) — schema deploy pipeline; tables in
  [Database Schema](Schema.md)

## Setup & Deployment

- [Development Setup](Development-Setup.md) — dev machine setup & running
  the stack locally
- [Deployment](Deployment.md) — `deploy.bat` publish flow + Windows
  services on the target
