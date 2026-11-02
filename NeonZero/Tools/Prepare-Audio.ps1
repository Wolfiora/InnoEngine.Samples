[CmdletBinding()]
param([string]$ProjectRoot = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Split-Path -Parent $PSScriptRoot }
Add-Type -TypeDefinition @'
using System;
using System.IO;
/// <summary>Creates offline PCM assets for the sample's native audio importer.</summary>
public static class StoryOfflineAudio {
    /// <summary>Writes one deterministic mono WAV clip.</summary>
    /// <param name="path">Destination file inside the sample project.</param>
    /// <param name="kind">Authored ambience or effect preset index.</param>
    /// <param name="seconds">Clip duration in seconds.</param>
    /// <param name="seed">Deterministic noise seed.</param>
    public static void Write(
        string path,
        int kind,
        double seconds,
        int seed
    ) {
        const int rate = 22050;
        int count = (int)(seconds * rate);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var random = new Random(seed);
        using(var file = new BinaryWriter(File.Create(path))) {
            file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(36 + count * 2);
            file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); file.Write(16); file.Write((short)1);
            file.Write((short)1); file.Write(rate); file.Write(rate * 2); file.Write((short)2); file.Write((short)16);
            file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(count * 2);
            double low = 0;
            for(int i=0;i<count;i++) {
                double t = (double)i/rate, noise = random.NextDouble()*2-1;
                low = low*.97 + noise*.03;
                double envelope = kind < 3 ? Math.Min(1, Math.Min(t*5,(seconds-t)*5)) : Math.Exp(-t*(2.5+kind*.16));
                double value;
                if(kind==0) value = noise*.09 + low*.4;
                else if(kind==1) value = low*.2 + Math.Sin(2*Math.PI*55*t)*.06 + Math.Sin(2*Math.PI*110*t)*.02;
                else if(kind==2) value = low*.7 * (.5+.3*Math.Sin(2*Math.PI*.25*t));
                else { double frequency = 130+kind*43;
                    value = Math.Sin(2*Math.PI*(frequency*t + (kind%3==0 ? 170*t*t : 0)))*.20 + low*.25 + noise*(kind%2==0 ? .1 : .025);
                    if(kind==12) value = (Math.Sin(2*Math.PI*440*t)+.4*Math.Sin(2*Math.PI*660*t)+.25*Math.Sin(2*Math.PI*880*t))*.17;
                }
                file.Write((short)(Math.Max(-.95,Math.Min(.95,value*envelope))*32767));
            }
        }
    }
}
'@
$ambience = @('Rain','MetroHum','RooftopWind')
for($i=0;$i -lt $ambience.Count;$i++) { [StoryOfflineAudio]::Write((Join-Path $ProjectRoot ('Assets\Audio\Ambience\'+$ambience[$i]+'.wav')),$i,12,19+$i) }
$effects = @('Footsteps','PowerOut','LampIgnite','SpellCharge','SpellSlash','Shield','Impact','GlassCrack','CircuitConnect','ClockBell','SealBreak','DawnChime')
for($i=0;$i -lt $effects.Count;$i++) { [StoryOfflineAudio]::Write((Join-Path $ProjectRoot ('Assets\Audio\Sfx\'+$effects[$i]+'.wav')),$i+3,1.5,101+$i) }
Write-Host 'Prepared 3 ambience loops and 12 authored synthetic effects.'
