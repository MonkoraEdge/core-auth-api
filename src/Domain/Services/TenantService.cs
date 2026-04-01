using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Domain.Validations.AccountPermissionValidation;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate;
using MonkoraEdge.Core.DotNet.Extensions;
using MonkoraEdge.Core.DotNet.Extensions.Validations;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenanttRepository _tenanttRepository;

    public TenantService(IUnitOfWork unitOfWork,
        ITenanttRepository tenanttRepository
    )
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenanttRepository = tenanttRepository ?? throw new ArgumentNullException(nameof(tenanttRepository));
        DateTimeExtensions.SetCultureInfo("en-US");
    }

    public async Task<CreateResponse> CreateTenantAsync(TenantCreateRequest request)
    {
        var results = await new TenantValidator().ValidateAsync(request);
        if (!results.IsValid)
            throw new DomainException("created_agreement", results.Errors.ToErrorFields());

        var tenant = new Tenant
        {
           
        };


        _tenanttRepository.Insert(tenant);
        _ = _unitOfWork.SaveChangesAsync();

        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteTenantAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<TenantResponse> GetTenantByIdAsync(Guid id)
    {
        var tenant = await _tenanttRepository.GetTenantByIdAsync(id);
        if (tenant is null)
            throw new DomainException("get_tenant", "Tenant not found");

        var result = new TenantResponse
        {
            Id = tenant.Id.ToString(),
        };

        return result;
    }

    public Task<DataSourceResponse<TenantDataSourceResponse>> GetTenantsAsync(TenantDataSourceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UpdateResponse> UpdateTenantAsync(Guid id, TenantUpdateRequest request)
    {
        throw new NotImplementedException();
    }
}