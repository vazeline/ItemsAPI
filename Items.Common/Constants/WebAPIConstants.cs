namespace Items.Common.Constants
{
    public static class WebAPIConstants
    {
        public static class JwtClaimTypes
        {
            public const string ContactType = "ContactType";
            public const string ContactId = "ContactId";
            public const string ContactFullName = "ContactFullName";
            public const string UserTemplateId = "UserTemplateId";
            public const string UserPermissions = "UserPermissions";

            public const string IntroducerIdForAgentPortalFiltering = "IntroducerIdForAgentPortalFiltering";
            public const string SolicitorIdForAgentPortalFiltering = "SolicitorIdForAgentPortalFiltering";

            public const string NascentFilteringTypeId = "NascentFilteringType";
            public const string DentalPracticeContactIdsForNascentFiltering = "DentalPracticeContactIdsForNascentFiltering";
            public const string NascentParentContactId = "NascentParentContactId";
        }
    }
}
