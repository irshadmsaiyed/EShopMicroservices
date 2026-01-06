namespace Catalog.API.Products.GetProductById;

public record GetProductByIdRequest(Guid Id) : IQuery<GetProductByIdResult>;

public record GetProductByIdResult(Product Product);

internal class GetProductByIdQueryHandler(IDocumentSession session)
    : IQueryHandler<GetProductByIdRequest, GetProductByIdResult>
{
    public async Task<GetProductByIdResult> Handle(GetProductByIdRequest query, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<Product>(query.Id, cancellationToken);

        if (product is null)
        {
            throw new ProductNotFoundException(query.Id);
        }

        return new GetProductByIdResult(product);
    }
}