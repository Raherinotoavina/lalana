# PostgreSQL 17 + PostGIS 3 from the official postgres image and Debian's packages.
# Multi-architecture (amd64 and arm64), unlike postgis/postgis which is amd64 only.
FROM postgres:17-bookworm

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-17-postgis-3 \
    && rm -rf /var/lib/apt/lists/*
