# Copyright 2025, Roger Brown
# Licensed under the MIT License.

function Get-UptimeSince
{
	if ($IsWindows -or ('Desktop' -eq $PSEdition))
	{
		(Get-CimInstance -ClassName 'Win32_OperatingSystem').LastBootupTime
	}
	else
	{
		if ($IsLinux)
		{
			if (Get-Command -Name 'uptime' -ErrorAction 'Ignore')
			{
				$dateTime = (uptime -s) -split ' '
				$list = New-Object -TypeName 'System.Collections.ArrayList'
				$null = $list.AddRange([int32[]]($dateTime[0] -split '-'))
				$null = $list.AddRange([int32[]]($dateTime[1] -split ':'))
				$null = $list.Add([System.DateTimeKind]::Local)
				New-Object -TypeName 'System.DateTime' -ArgumentList $list.ToArray()
			}
			else
			{
				(Get-Item -LiteralPath '/proc/1').CreationTime
			}
		}
		else
		{
			$hash = New-Object -TypeName 'System.Collections.Hashtable'
			$eq = $null
			$name = $null

			foreach ($v in (sysctl kern.boottime) -split ' ')
			{
				if ($v.EndsWith(','))
				{
					$v = $v.Substring(0, $v.Length - 1)
				}

				if ($eq -eq '=')
				{
					$hash.$name = $v
				}

				$name = $eq
				$eq = $v
			}

			[DateTime]::UnixEpoch.AddSeconds($hash.sec).AddMicroseconds($hash.usec).ToLocalTime()
		}
	}
}

Export-ModuleMember -Function Get-UptimeSince
