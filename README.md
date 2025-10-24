# 🚧 This README is under construction 🚧

---

# 📰 HappyHeadlines — Distributed News Platform

A microservice-based system demonstrating **caching**, **monitoring**, and **observability** across globally replicated news articles.  
Built as part of a software development course project.

---

## 🧱 Overview

HappyHeadlines replicates news articles and comments across multiple continents.  
It demonstrates:
- **Redis caching**
- **OpenTelemetry tracing (Zipkin)**
- **Prometheus metrics & Grafana dashboards**
- **Serilog centralized logging (Seq)**

---

## 🧩 Architecture

| Service | Description |
|----------|--------------|
| **ArticleService** | Handles CRUD for articles, global replication, and background cache refresh. |
| **CommentService** | Handles CRUD for comments and includes an LRU-based comment cache. |
| **ProfanityService** | Independent service for text moderation. |
| **CacheService** | Shared Redis abstraction used by other services. |
| **Shared** | Common DTOs, logging, metrics, and tracing setup (`MonitorService`). |

---

## ⚙️ Features

### 🧠 Caching
- **ArticleCache**
  - Background service loads **articles from the last 14 days**.
  - Refreshes every hour to keep Redis warm.
- **CommentCache**
  - Uses a **cache-miss approach** (only cached when first fetched).
  - Holds **comments for 30 most recently accessed articles**.
  - Applies **LRU eviction** to remove the least recently used key.

### 📊 Monitoring
- **Prometheus** collects metrics such as:
  - `*_cache_hits_total`
  - `*_cache_misses_total`
  - `*_requests_total`
- **Grafana** visualizes system performance and cache ratios.

### 🔍 Tracing
- **OpenTelemetry + Zipkin**
  - End-to-end traces show how services communicate.
  - Example: Creating a comment → Article lookup → Profanity check → DB save → Cache invalidation.

### 🪵 Logging
- **Serilog + Seq**
  - Centralized structured logs for each microservice.
  - Unified correlation IDs through `TraceId`.

---

## 🧩 Monitoring Setup

| Tool | URL |
|------|------|
| **Prometheus** | http://localhost:9090 |
| **Grafana** | http://localhost:3000 |
| **Zipkin** | http://localhost:9411 |
| **Seq** | http://localhost:5341 |

---

## 🧰 Tech Stack

| Area | Tool |
|------|------|
| Language | C# (.NET 9) |
| Database | SQL Server |
| Cache | Redis |
| Monitoring | Prometheus + Grafana |
| Tracing | OpenTelemetry + Zipkin |
| Logging | Serilog + Seq |
| Messaging | RabbitMQ |

---

