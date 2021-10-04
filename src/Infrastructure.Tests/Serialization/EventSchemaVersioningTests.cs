using Infrastructure.Serialization;
using System;
using System.Collections.Immutable;
using Xunit;

namespace Infrastructure.Tests.Serialization
{
    public abstract class given_serializer
    {
        protected IJsonSerializer sut = new NewtonsoftJsonSerializer();
    }

    public class given_a_flat_serialized_poco : given_serializer
    {
        private readonly string oldSerializedVersion = @"
{
    '$type': 'Infrastructure.Tests.Serialization.Product, Infrastructure.Tests',
    'id': 'a123',
    'name': 'chair'
}";


        [Fact]
        public void when_a_new_reference_field_is_added_to_schema_then_the_payload_in_old_version_is_deserialized_with_default_values()
        {
            var deserialized = this.sut.Deserialize<Product>(this.oldSerializedVersion);

            Assert.Equal("a123", deserialized.Id);
            Assert.Equal("chair", deserialized.Name);

            // reference field
            Assert.Equal(default(string), deserialized.Desc);
        }

        [Fact]
        public void when_a_new_value_field_id_added_to_schema_then_old_version_is_deserialized_with_default_value()
        {
            var deserialized = this.sut.Deserialize<Product>(this.oldSerializedVersion);

            Assert.Equal("a123", deserialized.Id);
            Assert.Equal("chair", deserialized.Name);

            // value field
            Assert.True(deserialized.DateAdded == default(DateTime)); 
        }
    }

    public class given_a_nested_serialized_poco : given_serializer
    {
        private readonly string oldSerializedVersion = @"
{
    '$type': 'Infrastructure.Tests.Serialization.ProductLine, Infrastructure.Tests',
    'product': {
        '$type': 'Infrastructure.Tests.Serialization.Product, Infrastructure.Tests',
        'id': 'a123',
        'name': 'chair'
    },
    'total': 100.0
}";

        [Fact]
        public void when_a_new_reference_field_is_added_to_schema_then_old_version_is_deserialized_with_default_value()
        {
            var deserialized = this.sut.Deserialize<ProductLine>(this.oldSerializedVersion);

            Assert.Equal("a123", deserialized.Product.Id);
            Assert.Equal("chair", deserialized.Product.Name);

            // reference field
            Assert.True(deserialized.Product.Desc == default(string));
        }

        [Fact]
        public void when_a_new_value_field_id_added_to_schema_then_old_version_is_deserialized_with_default_value()
        {
            var deserialized = this.sut.Deserialize<ProductLine>(this.oldSerializedVersion);

            Assert.Equal("a123", deserialized.Product.Id);
            Assert.Equal("chair", deserialized.Product.Name);

            // value field
            Assert.True(deserialized.Product.DateAdded == default(DateTime));
        }
    }

    public class given_an_outdated_consumer_with_stale_schema : given_serializer
    {
        // new added fields in serialized payload that current schema does not have: desc2
        private readonly string newerSerializedVersion = @"
{
    '$type': 'Infrastructure.Tests.Serialization.Product, Infrastructure.Tests',
    'id': 'a123', 
    'name': 'chair',
    'longNewDesc': 'simple chair'
}";

        [Fact]
        public void when_deserializing_an_updated_flat_schema_then_ignores_new_reference_and_value_fields_and_deleted_fields_have_default_values()
        {
            // Added: "Description" property to product.
            var deserialized = this.sut.Deserialize<Product>(this.newerSerializedVersion);

            Assert.Equal("a123", deserialized.Id);
            Assert.Equal("chair", deserialized.Name);
            Assert.True(deserialized.Desc == default(string));
            Assert.True(deserialized.DateAdded == default(DateTime));
        }
    }

    public class given_an_event_with_a_serialized_collection : given_serializer
    {
        private readonly string oldSerializedVersion = @"
{
    '$type': 'Infrastructure.Tests.Serialization.Order, Infrastructure.Tests',
    'lines': [
        {
            '$type': 'Infrastructure.Tests.Serialization.ProductLine, Infrastructure.Tests',
            'product': {
                '$type': 'Infrastructure.Tests.Serialization.Product, Infrastructure.Tests',
                'id': 'a123',
                'name': 'chair'
            },
            'total': 100.0
        },
        {
            '$type': 'Infrastructure.Tests.Serialization.ProductLine, Infrastructure.Tests',
            'product': {
                '$type': 'Infrastructure.Tests.Serialization.Product, Infrastructure.Tests',
                'id': 'b456',
                'name': 'table'
            },
            'total': 250.5
        }
    ]
}";
        [Fact]
        public void when_new_fields_are_added_to_schema_in_collection_then_the_payload_in_old_version_is_deserialized_with_default_values()
        {
            var order = this.sut.Deserialize<Order>(this.oldSerializedVersion);

            Assert.Equal(2, order.Lines.Count);
            //Assert.Equal(decimal.Parse("100,0"), order.Lines[0].Total);
            //Assert.Equal(decimal.Parse("250,5"), order.Lines[1].Total);
            Assert.Equal("a123", order.Lines[0].Product.Id);
            Assert.Equal("b456", order.Lines[1].Product.Id);
            Assert.Equal("chair", order.Lines[0].Product.Name);
            Assert.Equal("table", order.Lines[1].Product.Name);
            // New fields
            Assert.True(order.Lines[0].Product.Desc == default(string));
            Assert.True(order.Lines[0].Product.DateAdded == default(DateTime));
            Assert.True(order.Lines[1].Product.Desc == default(string));
            Assert.True(order.Lines[1].Product.DateAdded == default(DateTime));
        }
    }

    // HELPERS

    public class Product
    {
        public Product(string id, string name, string desc, DateTime dateAdded)
        {
            this.Id = id;
            this.Name = name;
            this.Desc = desc;
            this.DateAdded = dateAdded;
        }

        public string Id { get; }
        public string Name { get; }
        public string Desc { get; }
        public DateTime DateAdded { get; }
    }

    public class ProductLine
    {
        public ProductLine(Product product, decimal total)
        {
            this.Product = product;
            this.Total = total;
        }

        public Product Product { get; }
        public decimal Total { get; }
    }

    public class Order
    {
        public Order(ImmutableList<ProductLine> lines)
        {
            this.Lines = lines;
        }

        public ImmutableList<ProductLine> Lines { get; }
    }
}
