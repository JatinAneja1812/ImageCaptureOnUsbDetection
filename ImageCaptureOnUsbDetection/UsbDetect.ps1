# UsbDetect.ps1
# One-shot: wait for next *arrival* and then exit

# Cleanup old events/subscription (best-effort)
Get-Event -SourceIdentifier 'volumeChange' -ErrorAction SilentlyContinue |
    Remove-Event -ErrorAction SilentlyContinue
Unregister-Event -SourceIdentifier 'volumeChange' -ErrorAction SilentlyContinue

# New subscription
Register-WmiEvent -Class Win32_VolumeChangeEvent -SourceIdentifier 'volumeChange' | Out-Null
Write-Host 'Waiting for USB drive...'

while ($true) {
    # Wait for next volumeChange event
    $event = Wait-Event -SourceIdentifier 'volumeChange'

    # Remove that event from the queue
    Remove-Event -EventIdentifier $event.EventIdentifier -ErrorAction SilentlyContinue

    $newEvent = $event.SourceEventArgs.NewEvent
    $eventType   = $newEvent.EventType        # 2 = arrival
    $driveLetter = $newEvent.DriveName

    if ($eventType -ne 2 -or -not $driveLetter) {
        # Not a device arrival (e.g. removal or config change) – loop and wait again
        continue
    }

    Write-Host "USB drive detected on $driveLetter."

    $disk = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID = '$driveLetter'"

    $result = [PSCustomObject]@{
        DriveLetter = $driveLetter
        VolumeName  = $disk.VolumeName
        DriveType   = $disk.DriveType
        EventType   = $eventType
    }

    # Clean up subscription
    Unregister-Event -SourceIdentifier 'volumeChange' -ErrorAction SilentlyContinue

    # Return result to C#
    $result
    break
}
