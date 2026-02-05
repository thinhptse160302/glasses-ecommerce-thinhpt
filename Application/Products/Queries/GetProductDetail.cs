using Application.Core;
using Application.Products.DTOs;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Products.Queries;

/// <summary>
/// Retrieves detailed product information with optimized query execution.
/// Uses AsSplitQuery to prevent Cartesian explosion when loading multiple collections (Variants, Images).
/// ProjectTo ensures only required columns are selected, reducing bandwidth and memory usage.
/// </summary>
public sealed class GetProductDetail
{
    public sealed class Query : IRequest<Result<ProductDto>>
    {
        public required Guid Id { get; set; }
    }

    internal sealed class Handler(AppDbContext context, IMapper mapper) 
        : IRequestHandler<Query, Result<ProductDto>>
    {
        public async Task<Result<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            // AsSplitQuery generates separate SQL queries for each collection:
            // 1. Product + Category
            // 2. Product Images (ordered by DisplayOrder)
            // 3. Variants + Stock
            // 4. Variant Images (ordered by DisplayOrder)
            // This prevents Cartesian explosion from multiple LEFT JOINs
            var product = await context.Products
                .AsSplitQuery()
                .Where(p => p.Id == request.Id)
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                return Result<ProductDto>.Failure("Product not found.", 404);
            }

            return Result<ProductDto>.Success(product);
        }
    }
}
