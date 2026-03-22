using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataModel;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class FluentMappingPerEntityTests : TestBase
	{
		/// <summary>
		/// Builds a minimal DatabaseModel with the given entity names and generates source code
		/// using FluentMapping metadata with the specified per-entity option.
		/// </summary>
		private static string GenerateAllCode(string[] entityNames, bool perEntity)
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.Metadata                         = MetadataSource.FluentMapping;
			options.DataModel.GenerateFluentMappingPerEntity    = perEntity;
			options.DataModel.HasDefaultConstructor             = true;
			options.DataModel.HasConfigurationConstructor       = false;
			options.DataModel.HasUntypedOptionsConstructor      = false;
			options.DataModel.HasTypedOptionsConstructor        = false;
			options.DataModel.GenerateInitDataContextMethod     = false;
			options.DataModel.GenerateAssociations              = false;
			options.DataModel.GenerateAssociationExtensions     = false;
			options.CodeGeneration.EnableNullableReferenceTypes = false;
			options.CodeGeneration.ClassPerFile                 = false;

			var contextClass = new ClassModel("TestDataDB", "TestDataDB")
			{
				Namespace = "Test",
				Modifiers = Modifiers.Public | Modifiers.Partial,
				BaseType  = WellKnownTypes.LinqToDB.Data.DataConnection
			};

			var dataContext = new DataContextModel(contextClass)
			{
				HasDefaultConstructor        = true,
				HasConfigurationConstructor  = false,
				HasUntypedOptionsConstructor = false,
				HasTypedOptionsConstructor   = false,
			};

			foreach (var name in entityNames)
			{
				var entityMetadata = new EntityMetadata { Name = new SqlObjectName(name) };

				var entityClass = new ClassModel(name, name)
				{
					Namespace = "Test",
					Modifiers = Modifiers.Public,
				};

				var columnProperty = new PropertyModel("Id", WellKnownTypes.System.Int32)
				{
					Modifiers = Modifiers.Public,
					HasSetter = true,
					IsDefault = true
				};

				var columnMetadata = new ColumnMetadata
				{
					Name         = "Id",
					CanBeNull    = false,
					IsPrimaryKey = true,
					IsColumn     = true,
				};

				var entity = new EntityModel(entityMetadata, entityClass, null);
				entity.Columns.Add(new ColumnModel(columnMetadata, columnProperty));
				dataContext.Entities.Add(entity);
			}

			var model = new DatabaseModel(dataContext);

			var languageProvider = LanguageProviders.CSharp;
			var metadataBuilder = MetadataBuilders.GetMetadataBuilder(languageProvider, MetadataSource.FluentMapping);

			// Get a real SQLite sql builder for the code generator (only used for function name generation)
			var dataProvider = SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft);
			var sqlBuilder   = dataProvider.CreateSqlBuilder(dataProvider.MappingSchema, new LinqToDB.DataOptions());

			var generator = new DataModelGenerator(
				languageProvider,
				model,
				metadataBuilder,
				name => name,
				sqlBuilder,
				options);

			var files   = generator.ConvertToCodeModel();
			var sources = new Scaffolder(languageProvider, HumanizerNameConverter.Instance, options, null)
				.GenerateSourceCode(model, files);

			return string.Join("\n", sources.Select(s => s.Code));
		}

		[Test]
		public void PerEntityGeneratesExtensionMethods()
		{
			var allCode = GenerateAllCode(new[] { "Customer", "Order" }, perEntity: true);

			// Extension class should exist
			Assert.That(allCode, Does.Contain("static partial class FluentMappingExtensions"));

			// Per-entity methods should exist
			Assert.That(allCode, Does.Contain("FluentMappingBuilder MapCustomer(this FluentMappingBuilder builder)"));
			Assert.That(allCode, Does.Contain("FluentMappingBuilder MapOrder(this FluentMappingBuilder builder)"));

			// Static constructor should call the methods
			Assert.That(allCode, Does.Contain("builder.MapCustomer()"));
			Assert.That(allCode, Does.Contain("builder.MapOrder()"));

			// Methods should return builder
			Assert.That(allCode, Does.Contain("return builder;"));

			// builder.Build() should still be called
			Assert.That(allCode, Does.Contain("builder.Build()"));
		}

		[Test]
		public void InlineModeDoesNotGenerateExtensionClass()
		{
			var allCode = GenerateAllCode(new[] { "Customer" }, perEntity: false);

			// No extension class
			Assert.That(allCode, Does.Not.Contain("FluentMappingExtensions"));

			// No extension methods
			Assert.That(allCode, Does.Not.Contain("MapCustomer(this"));

			// Inline mapping should be in static constructor
			Assert.That(allCode, Does.Contain(".Entity<Customer>()"));
			Assert.That(allCode, Does.Contain("builder.Build()"));
		}

		[Test]
		public void PerEntityHandlesDuplicateEntityNames()
		{
			// Two entities with the same class name (e.g. from different schemas)
			// The name normalization should disambiguate them
			var allCode = GenerateAllCode(new[] { "Item", "Item" }, perEntity: true);

			// Extension class should exist
			Assert.That(allCode, Does.Contain("FluentMappingExtensions"));

			// builder.Build() should still be called
			Assert.That(allCode, Does.Contain("builder.Build()"));

			// Should have 2 Map methods (one potentially renamed to avoid conflict)
			var mapMethodCount = System.Text.RegularExpressions.Regex.Matches(
				allCode, @"FluentMappingBuilder Map\w+\(this FluentMappingBuilder").Count;
			Assert.That(mapMethodCount, Is.EqualTo(2), "Should have 2 per-entity mapping methods");
		}
	}
}
