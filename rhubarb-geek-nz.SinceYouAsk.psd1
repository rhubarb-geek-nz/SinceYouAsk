@{
	RootModule = 'SinceYouAsk.psm1'
	ModuleVersion = '1.0.0'
	GUID = '3d349527-8756-4b24-a71f-08236392b587'
	Author = 'Roger Brown'
	CompanyName = 'rhubarb-geek-nz'
	Copyright = 'Copyright © 2025 Roger Brown'
	Description = 'Tool for getting the boot time'
	FunctionsToExport = @('Get-UptimeSince')
	CmdletsToExport = @()
	VariablesToExport = '*'
	AliasesToExport = @()
	PrivateData = @{
		PSData = @{
			ProjectUri = 'https://github.com/rhubarb-geek-nz/SinceYouAsk'
		}
	}
}
