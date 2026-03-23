namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Specifies the layout format for generated entity properties.
	/// </summary>
	public enum EntityLayout
	{
		/// <summary>
		/// Table layout: property types, names, and attributes are padded with spaces to form aligned columns.
		/// This is the default layout.
		/// </summary>
		Table,
		/// <summary>
		/// List layout: each property on its own line with no alignment padding.
		/// Attributes are rendered on separate lines above the property.
		/// A blank line separates each property.
		/// </summary>
		List,
		/// <summary>
		/// Compact list layout: attributes are rendered inline (e.g. <c>[PrimaryKey, GeneratedKey]</c>) on a line above the property.
		/// No blank lines between properties, except before properties with XML documentation.
		/// </summary>
		ListCompact,
		/// <summary>
		/// Dense list layout: attributes are rendered inline on the same line as the property (e.g. <c>[PrimaryKey, GeneratedKey] public int Id { get; set; }</c>).
		/// No blank lines between properties, except before properties with XML documentation.
		/// </summary>
		ListDense,
	}
}
