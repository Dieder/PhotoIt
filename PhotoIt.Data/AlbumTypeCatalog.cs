namespace PhotoIt.Data;

public static class AlbumTypeCatalog
{
    private static AlbumMetadataFieldDefinition Text(string id, string label, bool summary = false) =>
        new(id, label, ShowOnCover: summary);

    private static AlbumMetadataFieldDefinition Date(string id, string label, bool summary = false) =>
        new(id, label, AlbumMetadataValueType.Date, ShowOnCover: summary);

    private static AlbumMetadataFieldDefinition Number(
        string id,
        string label,
        string? unit = null,
        bool summary = false) =>
        new(id, label, AlbumMetadataValueType.Integer, unit, summary);

    public static IReadOnlyList<AlbumTypeDefinition> All { get; } =
    [
        new("travel", "Travel", "✈", "Trips, destinations, and the people you travel with.",
            "Use collages, mixed photo sizes, and the existing route map where available.",
            [Text("travelers", "Travelers", true), Text("destinations", "Destinations", true)],
            [Text("location", "Location"), Text("transport", "Transport"), Number("distance", "Distance", "km"),
                Text("accommodation", "Accommodation")]),
        new("wedding", "Wedding", "♡", "A wedding day and the story around it.",
            "Use larger photos and generous whitespace.",
            [Text("couple", "Couple's names", true), Date("wedding-date", "Wedding date", true),
                Text("venue", "Venue", true), Number("guest-count", "Guests", summary: true)],
            [Text("event", "Event"), Text("location", "Location"), Text("participants", "Participants"),
                Text("messages", "Messages or notes")]),
        new("birthday-party", "Birthday Party", "♧", "A birthday celebration, its guests, and its highlights.",
            "Use playful layouts with room for celebration photos.",
            [Text("celebrant", "Celebrant", true), Date("birthday-date", "Birthday date", true),
                Number("age-celebrated", "Age celebrated", summary: true), Number("guest-count", "Guests", summary: true),
                Text("theme", "Theme"), Text("venue", "Venue")],
            [Text("activity", "Activity"), Number("guest-count", "Guests"), Text("notes", "Cake, gifts, or messages")]),
        new("baby-kids", "Baby & Kids", "♡", "A child's milestones, growth, and everyday memories.",
            "Arrange chapters as a milestone or age timeline.",
            [Text("child-name", "Child's name", true), Date("birth-date", "Birth date", true),
                Text("age-range", "Age range covered", true), Text("birth-details", "Birth details")],
            [Text("milestone", "Milestone", true), Number("age-months", "Age", "months"),
                Number("height", "Height", "cm"), Number("weight", "Weight", "g"), Text("anecdote", "Anecdote")]),
        new("food-cooking", "Food & Cooking", "♨", "Food, cooking sessions, dishes, and recipes.",
            "Pair photos with recipe details and ingredients where supported.",
            [Text("theme", "Collection theme", true), Text("cuisine", "Cuisine", true), Text("author", "Author")],
            [Text("dish", "Dish", true), Text("recipe", "Recipe"), Text("ingredients", "Ingredients"),
                Text("method", "Method"), Number("cook-time", "Cooking time", "min"), Number("servings", "Servings")]),
        new("family", "Family", "⌂", "Family members, events, and memories across the years.",
            "Organize by year, event, or family member.",
            [Text("family-name", "Family name", true), Text("people", "Family members", true),
                Text("date-range", "Date range", true)],
            [Text("event", "Event", true), Text("place", "Place"), Text("relationships", "People and relationships"),
                Text("memories", "Memories")]),
        new("graduation", "Graduation", "★", "A student's school journey and graduation.",
            "Build from school milestones through the ceremony and celebration.",
            [Text("graduate", "Graduate", true), Text("institution", "Institution", true),
                Text("qualification", "Qualification", true), Date("graduation-date", "Graduation date", true)],
            [Text("ceremony", "Ceremony"), Text("achievements", "Achievements", true), Text("awards", "Awards"),
                Text("messages", "Messages")]),
        new("sports", "Sports", "⚽", "A season of matches, competitions, and achievements.",
            "Organize by season and event; record results alongside action photos.",
            [Text("sport", "Sport", true), Text("team-athlete", "Team or athlete", true),
                Text("season", "Season", true)],
            [Text("match", "Match or event", true), Text("opponent", "Opponent"), Text("venue", "Venue"),
                Text("result", "Result"), Text("statistics", "Statistics"),
                Text("achievements", "Achievements")]),
        new("pet", "Pet", "♡", "A pet's milestones, activities, and adventures.",
            "Arrange chapters as a timeline of milestones and adventures.",
            [Text("pet-name", "Pet name", true), Text("species-breed", "Species or breed", true),
                Date("birth-adoption-date", "Birth or adoption date", true),
                Text("age-range", "Age range covered", true)],
            [Text("milestone", "Milestone", true), Text("activities", "Activities"), Text("training", "Training"),
                Text("anecdotes", "Anecdotes")]),
        new("nature-wildlife", "Nature & Wildlife", "❋", "Wildlife observations, habitats, and field notes.",
            "Organize by location, season, or species; use the existing map when route data is available.",
            [Text("region", "Region", true), Text("date-range", "Date range", true),
                Text("theme", "Theme"), Text("photographer", "Photographer")],
            [Text("species", "Species observed", true), Text("habitat", "Habitat"),
                Text("location", "Location"), Text("weather", "Weather")]),
        new("holiday", "Holiday", "✦", "Holiday traditions, hosts, and celebrations.",
            "Organize preparations, celebrations, and traditions as chapters.",
            [Text("holiday-name", "Holiday", true), Text("year", "Year", true), Text("hosts", "Hosts"),
                Text("location", "Location", true), Text("participants", "Participants", true),
                Text("traditions", "Traditions")],
            [Text("activity", "Activity", true), Text("participants", "Participants"),
                Text("menu", "Menu"), Text("memories", "Memories")]),
        new("corporate-events", "Corporate Events", "▣", "Company events, sessions, and takeaways.",
            "Use a session or agenda order with branded page layouts.",
            [Text("organization", "Organization", true), Text("event-name", "Event name", true),
                Text("purpose", "Purpose"), Text("date-range", "Event dates", true),
                Text("venue", "Venue", true), Number("attendees", "Attendees", summary: true)],
            [Text("session", "Session", true), Text("speaker", "Speaker"), Text("agenda-time", "Agenda time"),
                Text("presentation", "Presentation"), Text("takeaway", "Takeaway")]),
        new("school-years", "School Years", "⌂", "School years, activities, projects, and achievements.",
            "Organize school years by term and activity.",
            [Text("student-class", "Student or class", true), Text("school", "School", true),
                Text("school-year", "School year", true), Text("grade", "Grade or class", true)],
            [Text("term", "Term", true), Text("activity", "Activity"), Text("teacher", "Teacher"),
                Text("project", "Project"), Text("achievement", "Achievement")]),
        new("retirement", "Retirement", "⌖", "A career, farewell, and the next chapter.",
            "Arrange career milestones before the farewell and next chapter.",
            [Text("retiree", "Retiree", true), Date("retirement-date", "Retirement date", true),
                Text("career-summary", "Career summary"), Text("organizations", "Organizations", true),
                Number("years-service", "Years of service", "years", true)],
            [Text("career-milestone", "Career milestone", true), Text("role", "Role"),
                Text("colleague-message", "Colleague message"), Text("future-plan", "Future plan")]),
        new("bucket-list", "Bucket List Adventures", "✓", "Goals, challenges, destinations, and completed adventures.",
            "Present goals as a checklist or a timeline of completed adventures.",
            [Text("owner-participants", "Owner or participants", true), Text("theme", "Theme")],
            [Text("goal", "Goal", true), Text("completion-status", "Completion status"),
                Date("completion-date", "Completion date", true), Text("destination", "Destination"),
                Text("challenge", "Challenge"), Text("reflection", "Reflection")]),
        new("home-renovation", "Home Renovation", "⌂", "A property's renovation, phases, materials, and costs.",
            "Organize by room and renovation phase, with before-and-after photos.",
            [Text("project-property", "Project or property", true), Text("date-range", "Project dates", true),
                Text("scope", "Scope"), Text("budget-currency", "Budget and currency"), Text("currency", "Currency")],
            [Text("room", "Room", true), Text("phase", "Phase"), Text("before-after", "Before or after"),
                Text("tasks", "Tasks"), Text("materials", "Materials"), Number("cost", "Cost")]),
        new("recipe-food-journal", "Recipe & Food Journal", "♨", "Recipes, cooking attempts, and cuisine notes.",
            "Pair photos with recipe cards and cooking journal entries where supported.",
            [Text("author", "Author", true), Text("collection-theme", "Collection theme", true),
                Text("cuisines", "Cuisines", true)],
            [Text("recipe", "Recipe", true), Text("ingredients", "Ingredients"), Text("steps", "Steps"),
                Number("timings", "Timing", "min"), Number("servings", "Servings"),
                Number("attempts", "Cooking attempts"), Number("rating", "Rating")])
    ];

    public static AlbumTypeDefinition Default => All[0];

    public static AlbumTypeDefinition Get(string? id) =>
        All.FirstOrDefault(type => string.Equals(type.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? Default;

    public static string GetDefaultPageTemplateId(string? albumType) =>
        Get(albumType).Id == "travel"
            ? LayoutTemplateIds.TravelStyle
            : LayoutTemplateIds.OneBig;

    public static List<AlbumMetadataValue> SetValue(
        IEnumerable<AlbumMetadataValue>? values,
        string albumType,
        string fieldId,
        string? value)
    {
        var metadataKey = GetMetadataKey(albumType, fieldId);
        var result = values?.Select(item => new AlbumMetadataValue
        {
            Id = item.Id,
            FunctionalName = item.FunctionalName,
            FieldId = item.FieldId,
            Value = item.Value
        }).ToList() ?? [];
        var existing = result.FirstOrDefault(item =>
            string.Equals(item.FieldId, metadataKey, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(value))
            result.RemoveAll(item => string.Equals(item.FieldId, metadataKey, StringComparison.Ordinal));
        else if (existing is not null)
            existing.Value = value.Trim();
        else
            result.Add(new AlbumMetadataValue { FieldId = metadataKey, Value = value.Trim() });
        return result;
    }

    public static string? GetValue(
        IEnumerable<AlbumMetadataValue>? values,
        string albumType,
        string fieldId)
    {
        var metadataKey = GetMetadataKey(albumType, fieldId);
        return values?.FirstOrDefault(item => string.Equals(item.FieldId, metadataKey, StringComparison.Ordinal))?.Value;
    }

    public static string GetMetadataKey(string albumType, string fieldId) => $"{albumType}:{fieldId}";

    public static string? ValidateValues(
        IEnumerable<AlbumMetadataFieldDefinition> fields,
        IEnumerable<AlbumMetadataValue> values,
        string albumType)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(albumType);

        foreach (var field in fields)
        {
            var value = GetValue(values, albumType, field.Id);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (field.ValueType == AlbumMetadataValueType.Integer
                && !int.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                return $"{field.Label} must be a whole number.";
            if (field.ValueType == AlbumMetadataValueType.Date
                && !DateOnly.TryParseExact(value, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                return $"{field.Label} must be a valid date.";
        }

        return null;
    }
}

public sealed class AlbumMetadataValue
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? FunctionalName { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
