# Learning Journey

This folder collects deep engineering notes for CliniKey features. Each section is
written as a project learning artifact: part architecture explanation, part code
reading map, part senior-engineer commentary.

## Available Journeys

| Feature | Focus |
| --- | --- |
| [002 Identity & Authentication](./002-identity-auth/README.md) | ASP.NET Identity, JWT claims, refresh token rotation, role policies, tenant claim resolution, secured controllers, and auth testing strategy |
| [003 Tenant Provisioning](./003-tenant-provisioning/README.md) | Multi-tenant provisioning, schema isolation, Clean Architecture, CQRS, PostgreSQL search paths, lifecycle operations, and testing strategy |

## How To Read These Notes

Use them alongside the source code. The notes explain why the code exists and how
the pieces fit together; the implementation remains the source of truth.

For each feature, read:

1. The feature README for the big picture.
2. The architecture walkthrough for the deeper engineering reasoning.
3. The code reading guide when you want to study line by line.
