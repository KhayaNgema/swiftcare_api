// VolunteerContractDocument.cs

using System.Collections.Generic;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public sealed class VolunteerContractDocument
    {
        public string ContractReference { get; set; } = string.Empty;

        public string ContractVersion { get; set; } = "1.0";

        public string OrganisationName { get; set; } =
            "SwiftCare";

        public string OrganisationSymbol { get; set; } =
            "♥";

        public string DocumentTitle { get; set; } =
            "Volunteer Service Agreement";

        public string FooterText { get; set; } =
            "SwiftCare • Volunteer Service Agreement";

        public VolunteerContractStyles Styles { get; set; } =
            new();

        public List<VolunteerContractSection> Sections { get; set; } =
            new();

        public List<VolunteerContractSignature> Signatures { get; set; } =
            new();
    }

    public sealed class VolunteerContractSection
    {
        public int Number { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public List<string> Paragraphs { get; set; } =
            new();

        public List<string> Bullets { get; set; } =
            new();

        public List<VolunteerContractField> Fields { get; set; } =
            new();
    }

    public sealed class VolunteerContractField
    {
        public string Label { get; set; } =
            string.Empty;

        public string Value { get; set; } =
            string.Empty;
    }

    public sealed class VolunteerContractSignature
    {
        public string Party { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;
    }
}