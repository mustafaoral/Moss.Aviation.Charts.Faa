# Moss.Aviation.Charts.Faa

Small console app to download FAA charts for a given airport. Downloads the page `https://nfdc.faa.gov/nfdcApps/services/ajv5/airportDisplay.jsp?airportId=[icao-identifier]` to extract available charts.

## Environment variables

Environment variable `moss_aviation_charts_faa_download_path` is required to run the application. The value must be the path of an existing directory.

Environment variable `moss_aviation_charts_faa_app_publish_path` is optional if you want to use the included Powershell script `publish-local.ps1` to publish it locally.
