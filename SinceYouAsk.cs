// Copyright (c) 2025 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace RhubarbGeekNz.SinceYouAsk
{
    [Cmdlet(VerbsCommon.Get, "UptimeSince")]
    [OutputType(typeof(DateTime))]
    sealed public class GetUptimeSince : PSCmdlet
    {
        private static DateTime? bootTime;

        protected override void ProcessRecord()
        {
            if (!bootTime.HasValue)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (PowerShell powerShell = PowerShell.Create(RunspaceMode.CurrentRunspace))
                    {
                        powerShell.AddCommand("Get-CimInstance");
                        powerShell.AddParameter("ClassName", "Win32_OperatingSystem");
                        var cimInstance = powerShell.Invoke()[0].BaseObject;
                        var cimInstanceProperties = cimInstance.GetType().GetProperty("CimInstanceProperties").GetValue(cimInstance);
                        var cimProperty = cimInstanceProperties.GetType().GetProperty("Item").GetValue(cimInstanceProperties, new object[] { "LastBootupTime" });
                        bootTime = ((DateTime)cimProperty.GetType().GetProperty("Value").GetValue(cimProperty, null)).ToUniversalTime();
                    }
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        DateTime dateTime = new FileInfo("/proc/1").CreationTime;

                        if (dateTime.Year < 1971)
                        {
                            using (PowerShell powerShell = PowerShell.Create(RunspaceMode.CurrentRunspace))
                            {
                                powerShell.AddScript("/bin/sh -c 'uptime --since'");
                                string[] dateTimePair = powerShell.Invoke()[0].BaseObject.ToString().Split(' ');
                                int[] date = dateTimePair[0].Split('-').Select(Int32.Parse).ToArray();
                                int[] time = dateTimePair[1].Split(':').Select(Int32.Parse).ToArray();
                                dateTime = new DateTime(date[0], date[1], date[2], time[0], time[1], time[2], DateTimeKind.Local);
                            }
                        }

                        bootTime = dateTime.ToUniversalTime();
                    }
                    else
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Create("FreeBSD")))
                        {
                            var time = new timeval { tv_sec = 0, tv_usec = 0 };
                            var size = Marshal.SizeOf<timeval>();
                            sysctlbyname("kern.boottime", ref time, ref size, IntPtr.Zero, 0);
                            bootTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time.tv_sec).AddMilliseconds(time.tv_usec / 1000);
                        }
                        else
                        {
                            using (PowerShell powerShell = PowerShell.Create(RunspaceMode.CurrentRunspace))
                            {
                                powerShell.AddScript("/bin/sh -c 'sysctl kern.boottime'");
                                string[] values = powerShell.Invoke()[0].BaseObject.ToString().Split(' ');
                                Dictionary<string, string> dict = new Dictionary<string, string>();
                                string name = null, eq = null;

                                foreach (string value in values)
                                {
                                    string trim = value.Trim();

                                    if (trim.EndsWith(","))
                                    {
                                        trim = trim.Substring(0, trim.Length - 1);
                                    }

                                    if (trim.Length > 0)
                                    {
                                        if ("=".Equals(eq))
                                        {
                                            dict[name] = trim;
                                            name = null;
                                            eq = null;
                                        }
                                        else
                                        {
                                            name = eq;
                                            eq = value;
                                        }
                                    }
                                }

                                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                                if (dict.TryGetValue("sec", out string sec))
                                {
                                    dateTime = dateTime.AddSeconds(long.Parse(sec));
                                }

                                if (dict.TryGetValue("usec", out string usec))
                                {
                                    dateTime = dateTime.AddMilliseconds(long.Parse(usec) / 1000);
                                }

                                bootTime = dateTime;
                            }
                        }
                    }
                }
            }

            WriteObject(bootTime.Value.ToLocalTime());
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct timeval
        {
            public long tv_sec;
            public long tv_usec;
        }

        [DllImport("libc")]
        private static extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, ref timeval oldp, ref int oldlen, IntPtr newp, int newlen);
    }
}
