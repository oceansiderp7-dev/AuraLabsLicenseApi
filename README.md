# Aura Labs License API

This is the online license server for Aura Labs Packer.

## Required Environment Variables

Set these on your hosting provider:

```powershell
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_SERVICE_ROLE_KEY=your-service-role-key
AURA_ADMIN_PASSWORD=AuraLabsOntop2026$$$
AURA_LICENSE_SIGNING_SECRET=AuraLabsPacker::OfflineLicense::v2::R8T3N1Y6U4W2P9K5
```

Do not put `SUPABASE_SERVICE_ROLE_KEY` inside the desktop app.

## Run Locally

```powershell
cd "C:\Users\AuraL\Desktop\Aura APp\AuraLabsLicenseApi"
$env:SUPABASE_URL="https://your-project.supabase.co"
$env:SUPABASE_SERVICE_ROLE_KEY="your-service-role-key"
$env:AURA_ADMIN_PASSWORD="AuraLabsOntop2026$$$"
dotnet run
```

## Main Endpoints

- `POST /api/license/activate`
- `POST /api/admin/login`
- `GET /api/admin/licenses`
- `POST /api/admin/licenses/generate`
- `POST /api/admin/licenses/{licenseId}/revoke`
- `POST /api/admin/licenses/{licenseId}/restore`
- `POST /api/admin/licenses/{licenseId}/extend`
- `POST /api/admin/licenses/{licenseId}/reset-hwid`

Admin endpoints require this header:

```text
X-Admin-Password: AuraLabsOntop2026$$$
```
