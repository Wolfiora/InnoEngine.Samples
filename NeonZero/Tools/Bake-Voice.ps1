[CmdletBinding()]
param(
    [string]$ProjectRoot = '',
    [switch]$ProbeOnly
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Split-Path -Parent $PSScriptRoot }
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime]
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesisStream,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime]
$voices = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices
if ($ProbeOnly) { $voices | Select-Object DisplayName,Language,Id; return }
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
} | Select-Object -First 1
if ($null -eq $asTask) { throw 'The public WinRT asynchronous operation adapter is unavailable.' }
$awaitSpeech = $asTask.MakeGenericMethod([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
$casting = @{lin='Yaoyao';xu='Huihui';shen='Kangkang'}
$synthesizers = @{}
try {
    foreach ($character in $casting.Keys) {
        $voice = $voices | Where-Object { $_.DisplayName -like ('*'+$casting[$character]+'*') } | Select-Object -First 1
        if ($null -eq $voice) { throw ('Missing installed Chinese voice: '+$casting[$character]) }
        $synthesizer = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
        $synthesizer.Voice = $voice
        $synthesizers[$character] = $synthesizer
    }
    $rows = Import-Csv -LiteralPath (Join-Path $ProjectRoot 'artifacts\Exports\Voice\VoiceScript.tsv') -Delimiter "`t" -Encoding UTF8
    $count = 0
    foreach ($row in $rows) {
        $folder = Join-Path $ProjectRoot ('Assets\Audio\Voice\zh-CN\'+$row.chapter)
        New-Item -ItemType Directory -Force $folder | Out-Null
        $output = Join-Path $folder ($row.textId+'.wav')
        if ((Test-Path -LiteralPath $output) -and (Get-Item -LiteralPath $output).Length -gt 44) { $count++; continue }
        $operation = $synthesizers[$row.character].SynthesizeTextToStreamAsync($row.text)
        $task = $awaitSpeech.Invoke($null,@($operation))
        $task.Wait()
        $speech = $task.Result
        try {
            $input = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($speech)
            try {
                $file = [IO.File]::Create($output)
                try { $input.CopyTo($file) } finally { $file.Dispose() }
            } finally { $input.Dispose() }
        } finally { $speech.Dispose() }
        if ((Get-Item -LiteralPath $output).Length -le 44) { throw ('Empty synthesized voice: '+$row.textId) }
        $count++
        Write-Host ($count.ToString()+'/'+$rows.Count+' '+$row.textId)
    }
    Write-Host ('Synthesized dialogue files: '+$count)
} finally {
    foreach ($synthesizer in $synthesizers.Values) { $synthesizer.Dispose() }
}
