using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate.Setting;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Domain.Validations.AccountPermissionValidation;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.DataSourceAggregate;
using MonkoraEdge.Core.DotNet.Extensions.Validations;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantRepository _tenantRepository;

    public TenantService(
        IUnitOfWork unitOfWork,
        ITenantRepository tenantRepository)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    }

    public async Task<CreateResponse> CreateTenantAsync(TenantCreateRequest request)
    {
        var results = await new TenantValidator().ValidateAsync(request);
        if (!results.IsValid)
            throw new DomainException("create_tenant", results.Errors.ToErrorFields());

        var code = request.TenantCode.ToUpperInvariant().Trim();

        var existing = await _tenantRepository.GetListAsync(code, null);
        if (existing.Any(t => t.TenantCode == code))
            throw new DomainException("create_tenant", $"Tenant code '{code}' already exists.");

        var tenant = new Tenant
        {
            TenantCode = code,
            TenantName = request.TenantName,
            Settings = request.Settings ?? new TenantSettings(),
            IsActive = request.IsActive
        };

        _tenantRepository.Insert(tenant);
        await _unitOfWork.SaveChangesAsync();

        return new CreateResponse { Id = tenant.Id, IsSuccess = true, Message = "Tenant created." };
    }

    public async Task<DataSourceResponse<TenantDataSourceResponse>> GetTenantsAsync(TenantDataSourceRequest request)
    {
        var all = await _tenantRepository.GetListAsync(request.Keyword, request.IsActive);

        var total = all.Count;
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize < 1 ? 20 : request.PageSize;
        var data = all
            .Skip((page - 1) * size)
            .Take(size)
            .Select(MapToDataSourceResponse)
            .ToList();

        return new DataSourceResponse<TenantDataSourceResponse>
        {
            Data = data,
            TotalRecords = total,
            RecordsPerPage = size
        };
    }

    public async Task<TenantResponse> GetTenantByIdAsync(Guid id)
    {
        var tenant = await _tenantRepository.GetTenantByIdAsync(id);
        if (tenant is null)
            throw new DomainException("get_tenant", "Tenant not found.");

        return MapToResponse(tenant);
    }

    public async Task<UpdateResponse> UpdateTenantAsync(Guid id, TenantUpdateRequest request)
    {
        var tenant = await _tenantRepository.GetTenantByIdAsync(id);
        if (tenant is null)
            throw new DomainException("update_tenant", "Tenant not found.");

        if (request.TenantName != null)
            tenant.TenantName = request.TenantName;

        if (request.IsActive.HasValue)
            tenant.IsActive = request.IsActive.Value;

        if (request.Settings != null)
            tenant.Settings = request.Settings;

        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync();

        return new UpdateResponse { Id = tenant.Id, IsSuccess = true, Message = "Tenant updated." };
    }

    public async Task<DeleteResponse> DeleteTenantAsync(Guid id)
    {
        var tenant = await _tenantRepository.GetTenantByIdAsync(id);
        if (tenant is null)
            throw new DomainException("delete_tenant", "Tenant not found.");

        tenant.DeletedAt = DateTime.UtcNow;
        tenant.IsActive = false;
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync();

        return new DeleteResponse { Id = tenant.Id, IsSuccess = true, Message = "Tenant deleted." };
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static TenantResponse MapToResponse(Tenant t) => new()
    {
        Id = t.Id.ToString(),
        TenantCode = t.TenantCode,
        TenantName = t.TenantName,
        Settings = t.Settings,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static TenantDataSourceResponse MapToDataSourceResponse(Tenant t) => new()
    {
        Id = t.Id.ToString(),
        TenantCode = t.TenantCode,
        TenantName = t.TenantName,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };
}