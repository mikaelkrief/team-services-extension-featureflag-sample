import Q = require("q");
import * as ldservice from "./launchdarkly.service";

VSS.require("TFS/Dashboards/WidgetHelpers", function (WidgetHelpers) {
    WidgetHelpers.IncludeWidgetConfigurationStyles();

    let webContext = VSS.getWebContext();
    let user = {
        "key": webContext.account.name + ":" + webContext.user.email
    };

    ldservice.LaunchDarklyService.init(user).then((p) => {
        p.ldClient.on("ready", function () {

            VSS.register("widgetId-Configuration", function () {

                ldservice.LaunchDarklyService.setFlags();

                const CanShowQueryDetails = function () {
                    if (ldservice.LaunchDarklyService.flags["show-query-details"]) {
                        $("#panel-showquerydetails").show();
                    } else {
                        $("#panel-showquerydetails").hide();
                    }
                };

                const DisplayOrderModeList = function (enable: boolean) {
                    if (enable) {
                        $("#display-options").show();
                    } else {
                        $("#display-options").hide();
                    }
                };

                const SetEnableFF = function (enabled: boolean, feature: string): Promise<string> {
                    let deferred = Q.defer<string>();
                    ldservice.LaunchDarklyService.updateUserFeature(ldservice.LaunchDarklyService.user, enabled, feature).then((r) => {
                        deferred.resolve(r);
                    });
                    return deferred.promise;
                };

                CanShowQueryDetails();

                $("#enableff").prop("checked", ldservice.LaunchDarklyService.flags["mode-result-order"]);
                DisplayOrderModeList(ldservice.LaunchDarklyService.flags["mode-result-order"]);

                const $queryDropdown = $("#query-path");
                const $enableff = $("enableff");

                return {
                    load: function (widgetSettings, widgetConfigurationContext) {
                        const settings = JSON.parse(widgetSettings.customSettings.data);
                        if (settings && settings.queryPath) {
                            $queryDropdown.val(settings.queryPath);
                        }
                        $queryDropdown.on("change", function () {
                            const customSettings = {
                                data: JSON.stringify({
                                    queryPath: $queryDropdown.val()
                                })
                            };
                            const eventName = WidgetHelpers.WidgetEvent.ConfigurationChange;
                            const eventArgs = WidgetHelpers.WidgetEvent.Args(customSettings);
                            widgetConfigurationContext.notify(eventName, eventArgs);
                        });

                        $("#enableff").on("change", () => {
                            let enabledFeature = $("#enableff").is(":checked");
                            DisplayOrderModeList(enabledFeature);
                            SetEnableFF(enabledFeature, "mode-result-order").then((e) => {
                                if (e = "204") {
                                    ldservice.LaunchDarklyService.updateFlag("mode-result-order", enabledFeature);
                                    DisplayOrderModeList(enabledFeature);
                                    ldservice.LaunchDarklyService.trackEvent("mode-result-order");
                                }
                            });
                        });

                        return WidgetHelpers.WidgetStatusHelper.Success();
                    },
                    onSave: function () {
                        const customSettings = {
                            data: JSON.stringify({
                                queryPath: $queryDropdown.val()
                            })
                        };
                        return WidgetHelpers.WidgetConfigurationSave.Valid(customSettings);
                    }
                };
            });
            VSS.notifyLoadSucceeded();
        });
    });
});