# Smoke tests run after every Coolify deploy, against the DEPLOY_URL of the environment.
# Keep them fast, read-only and safe for production. Variable: {{base_url}}
GET {{base_url}}/health
HTTP 200
