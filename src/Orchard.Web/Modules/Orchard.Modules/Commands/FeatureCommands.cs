﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Environment.Commands;
using Orchard.Modules.Services;
using Orchard.Environment.Extensions.Features;
using Orchard.Environment.Shell.Descriptor.Models;
using Orchard.Utility;
using Orchard.Notifications;
using System.Threading.Tasks;

namespace Orchard.Modules.Commands
{
    public class FeatureCommands : DefaultOrchardCommandHandler
    {
        private readonly IModuleService _moduleService;
        private readonly IFeatureManager _featureManager;
        private readonly INotifier _notifier;
        private readonly ShellDescriptor _shellDescriptor;

        public FeatureCommands(IModuleService moduleService, 
            IFeatureManager featureManager,
            INotifier notifier,
            ShellDescriptor shellDescriptor)
        {
            _moduleService = moduleService;
            _featureManager = featureManager;
            _notifier = notifier;
            _shellDescriptor = shellDescriptor;
        }

        [OrchardSwitch]
        public bool Summary { get; set; }

        [CommandHelp("feature list [/Summary:true|false] [partial_feature_name]" + "\r\n\tDisplay list of available features" + "\r\n\tFilter the list by partial_feature_name, if given ")]
        [CommandName("feature list")]
        [OrchardSwitches("Summary")]
        public async Task List(params string[] featureNames)
        {
            var enabled = _shellDescriptor.Features.Select(x => x.Name);
            Func<string, bool> filter = featureNames.Any() ? new Func<string, bool>(f => f.IndexOf(featureNames[0], 0, StringComparison.OrdinalIgnoreCase) >= 0) : new Func<string, bool>(f => true);

            var features = await _featureManager.GetAvailableFeaturesAsync();
            if (Summary)
            {
                foreach (var feature in features.Where(f => filter(f.Name)).OrderBy(f => f.Id))
                {
                    Context.Output.WriteLine(T("{0}, {1}", feature.Id, enabled.Contains(feature.Id) ? T("Enabled") : T("Disabled")));
                }
            }
            else {
                Context.Output.WriteLine(T("List of available features"));
                Context.Output.WriteLine(T("--------------------------"));

                var categories = features.Where(f => filter(f.Name)).ToList().GroupBy(f => f.Category);
                foreach (var category in categories)
                {
                    Context.Output.WriteLine(T("Category: {0}", category.Key.OrDefault(T("General"))));
                    foreach (var feature in category.OrderBy(f => f.Id))
                    {
                        Context.Output.WriteLine(T("  Name: {0}", feature.Id));
                        Context.Output.WriteLine(T("    State:         {0}", enabled.Contains(feature.Id) ? T("Enabled") : T("Disabled")));
                        Context.Output.WriteLine(T("    Description:   {0}", feature.Description.OrDefault(T("<none>"))));
                        Context.Output.WriteLine(T("    Category:      {0}", feature.Category.OrDefault(T("<none>"))));
                        Context.Output.WriteLine(T("    Module:        {0}", feature.Extension.Id.OrDefault(T("<none>"))));
                        Context.Output.WriteLine(T("    Dependencies:  {0}", string.Join(", ", feature.Dependencies).OrDefault(T("<none>"))));
                    }
                }
            }
        }

        [CommandHelp("feature enable <feature-name-1> ... <feature-name-n>\r\n\t" + "Enable one or more features")]
        [CommandName("feature enable")]
        public async Task Enable(params string[] featureNames)
        {
            Context.Output.WriteLine(T("Enabling features {0}", string.Join(",", featureNames)));
            var listAvailableFeatures = false;
            var featuresToEnable = new List<string>();
            var features = await _featureManager.GetAvailableFeaturesAsync();
            var availableFeatures = features.Select(x => x.Id).OrderBy(x => x).ToArray();
            foreach (var featureName in featureNames)
            {
                if (availableFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase))
                {
                    featuresToEnable.Add(featureName);
                }
                else {
                    Context.Output.WriteLine(T("Could not find feature {0}", featureName));
                    listAvailableFeatures = true;
                }
            }
            if (featuresToEnable.Any())
            {
                await _moduleService.EnableFeatures(featuresToEnable, true);
                foreach (var entry in _notifier.List())
                {
                    Context.Output.WriteLine(entry.Message);
                }
            }
            else {
                Context.Output.WriteLine(T("Could not enable features: {0}", string.Join(",", featureNames)));
                listAvailableFeatures = true;
            }
            if (listAvailableFeatures)
            {
                Context.Output.WriteLine(T("Available features are : {0}", string.Join(",", availableFeatures)));
            }
        }

        [CommandHelp("feature disable <feature-name-1> ... <feature-name-n>\r\n\t" + "Disable one or more features")]
        [CommandName("feature disable")]
        public async Task Disable(params string[] featureNames)
        {
            Context.Output.WriteLine(T("Disabling features {0}", string.Join(",", featureNames)));
            await _moduleService.DisableFeatures(featureNames, true);
            Context.Output.WriteLine(T("Disabled features  {0}", string.Join(",", featureNames)));
        }
    }
}
