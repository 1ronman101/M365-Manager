namespace M365Manager.Models;

public class License
{
    public string SkuId { get; set; } = string.Empty;
    public string SkuPartNumber { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public int ConsumedUnits { get; set; }
    public int AvailableUnits => TotalUnits - ConsumedUnits;
    public bool HasAvailableUnits => AvailableUnits > 0;

    public string DisplayText => $"{FriendlyName} ({AvailableUnits} available)";

    /// <summary>
    /// Maps common SKU part numbers to human-friendly names.
    /// </summary>
    public static string GetFriendlyName(string skuPartNumber)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ENTERPRISEPACK"] = "Office 365 E3",
            ["ENTERPRISEPREMIUM"] = "Office 365 E5",
            ["ENTERPRISEPREMIUM_NOPSTNCONF"] = "Office 365 E5 (No PSTN)",
            ["SPE_E3"] = "Microsoft 365 E3",
            ["SPE_E5"] = "Microsoft 365 E5",
            ["SPE_F1"] = "Microsoft 365 F1",
            ["STANDARDPACK"] = "Office 365 E1",
            ["DESKLESSPACK"] = "Office 365 F3",
            ["O365_BUSINESS_ESSENTIALS"] = "Microsoft 365 Business Basic",
            ["O365_BUSINESS_PREMIUM"] = "Microsoft 365 Business Standard",
            ["SMB_BUSINESS_PREMIUM"] = "Microsoft 365 Business Premium",
            ["SMB_BUSINESS"] = "Microsoft 365 Apps for Business",
            ["OFFICESUBSCRIPTION"] = "Microsoft 365 Apps for Enterprise",
            ["EXCHANGESTANDARD"] = "Exchange Online (Plan 1)",
            ["EXCHANGEENTERPRISE"] = "Exchange Online (Plan 2)",
            ["EMS"] = "Enterprise Mobility + Security E3",
            ["EMSPREMIUM"] = "Enterprise Mobility + Security E5",
            ["PROJECTPREMIUM"] = "Project Plan 5",
            ["PROJECTPROFESSIONAL"] = "Project Plan 3",
            ["VISIOCLIENT"] = "Visio Plan 2",
            ["POWER_BI_STANDARD"] = "Power BI (Free)",
            ["POWER_BI_PRO"] = "Power BI Pro",
            ["FLOW_FREE"] = "Power Automate (Free)",
            ["TEAMS_EXPLORATORY"] = "Microsoft Teams Exploratory",
            ["AAD_PREMIUM"] = "Azure AD Premium P1",
            ["AAD_PREMIUM_P2"] = "Azure AD Premium P2",
            ["WIN10_PRO_ENT_SUB"] = "Windows 10/11 Enterprise E3",
            ["RIGHTSMANAGEMENT"] = "Azure Information Protection Plan 1",
            ["THREAT_INTELLIGENCE"] = "Office 365 Threat Intelligence",
            ["ATP_ENTERPRISE"] = "Microsoft Defender for Office 365 (Plan 1)",
            ["STREAM"] = "Microsoft Stream",
            ["MICROSOFT_BUSINESS_CENTER"] = "Microsoft Business Center",
            ["POWERAPPS_VIRAL"] = "Power Apps (Free)",
            ["WINDOWS_STORE"] = "Windows Store for Business",
        };

        return map.TryGetValue(skuPartNumber, out var friendly) ? friendly : skuPartNumber;
    }
}
