<#
.SYNOPSIS
    Seeds the LOCAL database with test users, a vehicle, a run and today's trips, routed.

.DESCRIPTION
    Local development only. It refuses to run against anything that is not localhost: the
    accounts it creates have a published password.

    What it creates (only what is missing, so it can be run again):
      - test.admin  (Admin, no Provider or Integrator)   password: test
      - test.driver (Driver)                              password: test
      - vehicle group, vehicle type, capacity type and the vehicle TEST-VAN
      - the run TEST-RUN for test.driver, smartphone login "test.driver", from today on
      - three fictitious patients, and today's trips for them, routed on TEST-RUN in order

    Everything goes through the local API except the first admin, which has to exist before
    anyone can sign in. Distances and ETAs are fixed values: nothing is asked of Google, so it
    works with no internet.

    -Reset deletes today's trips of the test patients (and their stops) before creating them.

.EXAMPLE
    .\Seed-Local.ps1
    .\Seed-Local.ps1 -Reset
#>
[CmdletBinding()]
param(
    [string] $ApiUrl = 'http://localhost:5241/',
    [string] $SqlServer = 'localhost',
    [string] $Database = 'RaphaelDB',
    [switch] $Reset
)

$ErrorActionPreference = 'Stop'

# ---- Guard: local only ---------------------------------------------------------------------
$apiHost = ([Uri]$ApiUrl).Host
if ($apiHost -notin @('localhost', '127.0.0.1', '::1') -or $SqlServer -notin @('localhost', '.', '(local)', '127.0.0.1')) {
    throw "Seed-Local only runs against localhost (API '$apiHost', SQL '$SqlServer'). Its accounts have a published password."
}

$TestPassword = 'test'
$Today = (Get-Date).Date
$TodayIso = $Today.ToString('yyyy-MM-dd')

function Invoke-Sql([string] $query) {
    $out = & sqlcmd -S $SqlServer -E -C -I -d $Database -h -1 -W -b -Q "SET NOCOUNT ON; $query"
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $query`n$out" }
    return ($out | Where-Object { $_ -ne '' })
}

function Get-Hash([string] $password) {
    # Same as Raphael.Shared.Helpers.PasswordHasher (SHA-256, base64; unsalted, see SECURITY_FINDINGS F10).
    $sha = [System.Security.Cryptography.SHA256]::Create()
    [Convert]::ToBase64String($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($password)))
}

# ---- 1. The first admin, straight into the database ----------------------------------------
$hash = Get-Hash $TestPassword
Invoke-Sql @"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'test.admin')
    INSERT INTO Users (FullName, Username, PasswordHash, IsActive, MustChangePassword, RoleId)
    VALUES ('Test Admin', 'test.admin', '$hash', 1, 0, 1);
ELSE
    UPDATE Users SET PasswordHash = '$hash', IsActive = 1, MustChangePassword = 0, RoleId = 1,
                     ProviderId = NULL, IntegratorId = NULL
    WHERE Username = 'test.admin';
"@ | Out-Null
Write-Host 'test.admin ready'

# ---- 2. Sign in as test.admin --------------------------------------------------------------
$login = Invoke-RestMethod -Method Post -Uri "${ApiUrl}api/Auth/login" -ContentType 'application/json' `
    -Body (@{ username = 'test.admin'; password = $TestPassword } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.token)" }

function Api([string] $method, [string] $path, $body = $null) {
    $req = @{ Method = $method; Uri = "$ApiUrl$path"; Headers = $headers; ContentType = 'application/json' }
    if ($null -ne $body) { $req.Body = ($body | ConvertTo-Json -Depth 6) }
    try {
        Invoke-RestMethod @req
    }
    catch {
        $detail = ''
        if ($_.Exception.Response) {
            $detail = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd()
        }
        throw "$method $path failed: $($_.Exception.Message) $detail"
    }
}

function Get-Id([string] $query) {
    $v = Invoke-Sql $query | Select-Object -First 1
    if ($v) { [int]$v } else { $null }
}

# ---- 3. test.driver --------------------------------------------------------------------------
if (-not (Get-Id "SELECT Id FROM Users WHERE Username = 'test.driver'")) {
    Api Post 'api/Users/create' @{ fullName = 'Test Driver'; username = 'test.driver'; password = $TestPassword; roleId = 2 } | Out-Null
}
Invoke-Sql "UPDATE Users SET PasswordHash = '$hash', IsActive = 1, MustChangePassword = 0 WHERE Username = 'test.driver';" | Out-Null
$driverId = Get-Id "SELECT Id FROM Users WHERE Username = 'test.driver'"
Write-Host "test.driver ready (id $driverId)"

# ---- 4. Vehicle ------------------------------------------------------------------------------
if (-not (Get-Id "SELECT Id FROM VehicleGroups WHERE Name = 'TEST'")) {
    Api Post 'api/VehicleGroups' @{ name = 'TEST'; description = 'Local test data'; color = '#7C3AED' } | Out-Null
}
if (-not (Get-Id "SELECT Id FROM VehicleTypes WHERE Name = 'TEST'")) {
    Api Post 'api/VehicleTypes' @{ name = 'TEST'; description = 'Local test data' } | Out-Null
}
if (-not (Get-Id "SELECT Id FROM CapacityDetailTypes WHERE Name = 'TEST'")) {
    Api Post 'api/CapacityDetailType' @{ name = 'TEST'; description = 'Local test data' } | Out-Null
}
if (-not (Get-Id "SELECT Id FROM Vehicles WHERE Name = 'TEST-VAN'")) {
    Api Post 'api/Vehicles' @{
        name = 'TEST-VAN'; make = 'Test'; model = 'Van'; plate = 'TEST001'; isInactive = $false
        groupId = (Get-Id "SELECT Id FROM VehicleGroups WHERE Name = 'TEST'")
        vehicleTypeId = (Get-Id "SELECT Id FROM VehicleTypes WHERE Name = 'TEST'")
        capacityDetailTypeId = (Get-Id "SELECT Id FROM CapacityDetailTypes WHERE Name = 'TEST'")
    } | Out-Null
}
$vehicleId = Get-Id "SELECT Id FROM Vehicles WHERE Name = 'TEST-VAN'"
Write-Host "TEST-VAN ready (id $vehicleId)"

# ---- 5. Run ----------------------------------------------------------------------------------
# Garage: downtown Miami. From today, all day.
if (-not (Get-Id "SELECT Id FROM VehicleRoutes WHERE Name = 'TEST-RUN'")) {
    Api Post 'api/Runs' @{
        name = 'TEST-RUN'; description = 'Local test data'; driverId = $driverId; vehicleId = $vehicleId
        garage = '100 Test Garage St, Miami, FL 33130'; garageLatitude = 25.7617; garageLongitude = -80.1918
        smartphoneLogin = 'test.driver'; fromDate = $TodayIso; fromTime = '05:00:00'; toTime = '23:00:00'
    } | Out-Null
}
$runId = Get-Id "SELECT Id FROM VehicleRoutes WHERE Name = 'TEST-RUN'"
Write-Host "TEST-RUN ready (id $runId)"

# ---- 6. Fictitious patients ------------------------------------------------------------------
$fundingSourceId = Get-Id 'SELECT TOP 1 Id FROM FundingSources ORDER BY Id'
$spaceTypeId = Get-Id 'SELECT TOP 1 Id FROM SpaceTypes ORDER BY Id'

$patients = @(
    @{ name = 'Test Patient Alpha';   address = '201 Test Ave, Miami Beach, FL 33139'; lat = 25.7907; lng = -80.1300 }
    @{ name = 'Test Patient Bravo';   address = '302 Test Blvd, Miami, FL 33142';      lat = 25.8136; lng = -80.2245 }
    @{ name = 'Test Patient Charlie'; address = '403 Test Rd, Miami, FL 33145';        lat = 25.7470; lng = -80.2570 }
)
foreach ($p in $patients) {
    if (-not (Get-Id "SELECT Id FROM Customers WHERE FullName = '$($p.name)'")) {
        Api Post 'api/Customers' @{
            fullName = $p.name; address = $p.address; city = 'Miami'; state = 'FL'; zip = '33130'
            phone = '3050000000'; gender = 'Other'; fundingSourceId = $fundingSourceId; spaceTypeId = $spaceTypeId
            created = (Get-Date).ToUniversalTime().ToString('o'); createdBy = 'seed-local'
            latitude = $p.lat; longitude = $p.lng
        } | Out-Null
    }
    $p.id = Get-Id "SELECT Id FROM Customers WHERE FullName = '$($p.name)'"
}
Write-Host 'Patients ready'

# ---- 7. Today's trips ------------------------------------------------------------------------
$customerIds = ($patients | ForEach-Object { $_.id }) -join ','
if ($Reset) {
    Invoke-Sql @"
DECLARE @t TABLE (Id int);
INSERT INTO @t SELECT Id FROM Trips WHERE CustomerId IN ($customerIds) AND CAST([Date] AS date) = '$TodayIso';
DELETE FROM TripLogs WHERE TripId IN (SELECT Id FROM @t);
DELETE FROM TripStopNotes WHERE TripId IN (SELECT Id FROM @t);
DELETE FROM Schedules WHERE TripId IN (SELECT Id FROM @t);
DELETE FROM Schedules WHERE VehicleRouteId = $runId AND CAST([Date] AS date) = '$TodayIso';
DELETE FROM Trips WHERE Id IN (SELECT Id FROM @t);
"@ | Out-Null
    Write-Host "Today's test trips deleted"
}

$existing = Get-Id "SELECT COUNT(*) FROM Trips WHERE CustomerId IN ($customerIds) AND CAST([Date] AS date) = '$TodayIso'"
if ($existing -gt 0) {
    Write-Host "Today already has $existing test trips. Use -Reset to recreate them."
    return
}

# One clinic for everybody: the dropoff.
$clinic = @{ address = '500 Test Clinic Way, Miami, FL 33136'; lat = 25.7906; lng = -80.2125 }

# Pickups an hour apart from now, so they are ahead of the clock whenever this runs.
$start = (Get-Date).AddMinutes(30)
$start = $start.Date.AddHours($start.Hour).AddMinutes([Math]::Ceiling($start.Minute / 15) * 15)

$i = 0
foreach ($p in $patients) {
    $pickup = $start.AddHours($i)
    $appt = $pickup.AddMinutes(45)
    $trip = Api Post 'api/Trips' @{
        day = $Today.DayOfWeek.ToString(); date = $TodayIso
        fromTime = $pickup.ToString('HH:mm:ss'); toTime = $appt.ToString('HH:mm:ss')
        customerId = $p.id; type = 'Appointment'; willCall = $false; status = 'Accepted'
        pickupAddress = $p.address; pickupLatitude = $p.lat; pickupLongitude = $p.lng; pickupCity = 'Miami'
        dropoffAddress = $clinic.address; dropoffLatitude = $clinic.lat; dropoffLongitude = $clinic.lng; dropoffCity = 'Miami'
        spaceTypeId = $spaceTypeId; fundingSourceId = $fundingSourceId; distance = 6.5
        pickupComment = 'Seeded test trip'; dropoffComment = 'Seeded test trip'
    }
    $tripId = if ($trip.id) { $trip.id } else { Get-Id "SELECT TOP 1 Id FROM Trips WHERE CustomerId = $($p.id) AND CAST([Date] AS date) = '$TodayIso' ORDER BY Id DESC" }

    # Fixed distances and times: no Google.
    Api Post 'api/Schedules/route' @{
        vehicleRouteId = $runId; tripId = $tripId
        pickupDistance = 5.0; pickupTravelTime = '00:15:00'; pickupETA = $pickup.ToString('HH:mm:ss')
        dropoffDistance = 6.5; dropoffTravelTime = '00:20:00'; dropoffETA = $pickup.AddMinutes(35).ToString('HH:mm:ss')
        returnToGarageTravelTime = '00:15:00'; targetSequence = 2 * $i + 1
    } | Out-Null
    Write-Host ("Trip {0} for {1}: pickup {2}, routed" -f $tripId, $p.name, $pickup.ToString('HH:mm'))
    $i++
}

Write-Host ''
Write-Host "Done. Sign in on the Driver with test.driver / $TestPassword; the Desktop with test.admin / $TestPassword."
