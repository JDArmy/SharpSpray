using CommandLine;
using CommandLine.Text;
using SharpHose.Common.Enums;
using SharpHose.Common.Helpers;
using SharpHose.Common.Objects;
using SharpHose.Nozzles.LDAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpHose
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.HelpWriter = null;
                config.CaseInsensitiveEnumValues = true;
            });
            var parserResult = parser.ParseArguments<CLIOptions>(args);

            parserResult
              .WithParsed<CLIOptions>(options => Run(options))
              .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = true;
                h.Copyright = string.Empty;
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e);
            Console.WriteLine(helpText);

            foreach (var error in errs)
            {
                switch (error.Tag)
                {
                    case ErrorType.MissingRequiredOptionError:
                        var requiredError = (MissingRequiredOptionError)error;
                        Console.WriteLine($"Error missing argument: {requiredError.NameInfo.NameText}");
                        break;
                    case ErrorType.MissingValueOptionError:
                        var valueError = (MissingValueOptionError)error;
                        Console.WriteLine($"Error missing required value: {valueError.NameInfo.NameText}");
                        break;
                }
            }

            var usage = "\nExamples:\n";
            usage += $"Domain Joined Spray: SharpSpray.exe --action SPRAY_USERS --spraypassword Spring2020! \n";
            usage += $"Domain Joined Spray w/ Exclusions: SharpSpray.exe --action SPRAY_USERS --auto\n";
            usage += $"Domain Joined Spray w/ Exclusions: SharpSpray.exe --action SPRAY_USERS --passfile c:\\test.txt\n";
            usage += $"Non-Domain Joined Spray: SharpSpray.exe  --dc-ip 172.16.178.9 --domain windows.local --username test --password Aa123456789. --action SPRAY_USERS --spraypassword Aa123456789.\n";
            usage += $"Domain Joined Show Policies: SharpSpray.exe --action GET_POLICIES \n";
            usage += $"Domain Joined Show Policy Users: SharpSpray.exe --action GET_POLICY_USERS --policy windows \n";
            usage += $"Domain Joined Show All Users: SharpSpray.exe --action GET_ENABLED_USERS \n";
            Console.Write(usage);
        }

        static void Run(CLIOptions opts)
        {


            if ((string.IsNullOrEmpty(opts.PolicyName)) && (opts.Action == LDAPAction.GET_POLICY_USERS))
            {

                    Console.WriteLine($"Missing --policy argument.");
                    Environment.Exit(0);
                
            }

            if((string.IsNullOrEmpty(opts.OutputPath)) && (opts.Quiet))
            {

                    Console.WriteLine($"Missing --output argument while using --quiet.");
                    Environment.Exit(0);
                
            }

            var config = new LDAPConfig()
            {
                DomainName = opts.DomainName,
                DomainController = opts.DomainController,
                DomainUsername = opts.DomainUsername,
                DomainPassword = opts.DomainPassword,
                SprayPassword = opts.SprayPassword,
                OutputPath = opts.OutputPath,
                Auto = opts.Auto,
                ExcludeFilePath = opts.ExcludeFilePath,
                SaveOutput = !string.IsNullOrEmpty(opts.OutputPath),
                ExcludeUsers = !string.IsNullOrEmpty(opts.ExcludeFilePath),
                Logger = new ConsoleLogger(opts.Quiet),
                PasswdFilePath = opts.PasswdFilePath,
                DomainIp = opts.DomainIp
            };

            switch (opts.Nozzle)
            {
                case NozzleType.LDAP:
                    var ldapNozzle = new LDAPNozzle(config);

                    switch(opts.Action)
                    {
                        case LDAPAction.GET_POLICIES:
                            ldapNozzle.DisplayPolicies();
                            break;
                        case LDAPAction.GET_POLICY_USERS:
                            ldapNozzle.DisplayPolicyUsers(opts.PolicyName, false);
                            break;
                        case LDAPAction.GET_ENABLED_USERS:
                            ldapNozzle.DisplayEnabledUsers();
                            break;
                        case LDAPAction.SPRAY_USERS:
                            ldapNozzle.Start();
                            break;
                    }
                    break;
            }
        }
    }
}
