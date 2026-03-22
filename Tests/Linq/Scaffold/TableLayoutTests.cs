using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class TableLayoutTests : TestBase
	{
		/// <summary>
		/// Generates source code for a class with two properties of different type/name lengths,
		/// using the specified <paramref name="tableLayout"/> setting on the property group.
		/// </summary>
		private static string GenerateProperties(bool tableLayout)
		{
			var lang    = LanguageProviders.CSharp;
			var builder = lang.ASTBuilder;

			var file = builder.File("Test");
			var ns   = builder.Namespace("TestNamespace");
			file.Add(ns.Namespace);

			var classGroup = ns.Classes();
			var cls        = classGroup.New(new CodeIdentifier("TestEntity", true));
			cls.SetModifiers(Modifiers.Public);

			var props = cls.Properties(tableLayout);

			// short type + long name vs long type + short name — alignment padding is visible
			props.New(new CodeIdentifier("Id", true), WellKnownTypes.System.Int32)
				.SetModifiers(Modifiers.Public)
				.Default(true);
			props.New(new CodeIdentifier("LongPropertyName", true), WellKnownTypes.System.String)
				.SetModifiers(Modifiers.Public)
				.Default(true);

			var emptyDict1 = new Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>>();
			var emptyDict2 = new Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>();

			var codeGenerator = lang.GetCodeGenerator(
				"\n",
				"\t",
				useNRT: false,
				emptyDict1,
				emptyDict2,
				emptyDict2);

			codeGenerator.Visit(file);
			return codeGenerator.GetResult();
		}

		[Test]
		public void TableLayout_ContainsPaddedColumns()
		{
			var result = GenerateProperties(tableLayout: true);

			var lines = result.Split('\n')
				.Select(l => l.TrimEnd('\r'))
				.Where(l => l.Contains("{ get;"))
				.ToArray();

			Assert.That(lines, Has.Length.EqualTo(2), "Expected 2 property lines");

			var intLine = lines.First(l => l.Contains("Id"));

			// In table layout, "int" is padded with extra spaces to align with "string":
			// "public int    Id               { get; set; }"
			// "public string LongPropertyName { get; set; }"
			Assert.That(intLine, Does.Match(@"int\s{2,}"), "int should be padded with extra spaces in table layout mode");
		}

		[Test]
		public void NoTableLayout_HasNoPadding()
		{
			var result = GenerateProperties(tableLayout: false);

			var lines = result.Split('\n')
				.Select(l => l.TrimEnd('\r'))
				.Where(l => l.Contains("{ get;"))
				.ToArray();

			Assert.That(lines, Has.Length.EqualTo(2), "Expected 2 property lines");

			var intLine = lines.First(l => l.Contains("Id"));

			// Without table layout, "int" is immediately followed by a single space then "Id"
			Assert.That(intLine, Does.Contain("int Id"), "int should be followed by single space + Id without table layout");
			Assert.That(intLine, Does.Not.Match(@"int\s{2,}Id"), "int should not have extra padding before Id without table layout");
		}

		[Test]
		public void TableLayoutOnAndOff_ProduceDifferentOutput()
		{
			var aligned   = GenerateProperties(tableLayout: true);
			var unaligned = GenerateProperties(tableLayout: false);

			Assert.That(aligned, Is.Not.EqualTo(unaligned), "Table layout on and off should produce different output when properties have varying type/name lengths");
		}
	}
}
