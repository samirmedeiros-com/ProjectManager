using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface ISeurTabelasService
{
    // CPOSTAL
    Task<List<SeurCpostalDto>> GetCpostaisAsync(string? search, int page = 1, int pageSize = 100);
    Task<int> CountCpostaisAsync(string? search);
    Task<SeurCpostalDto?> GetCpostalAsync(long idt);
    Task<SeurCpostalDto> CreateCpostalAsync(SaveCpostalDto dto);
    Task<SeurCpostalDto?> UpdateCpostalAsync(long idt, SaveCpostalDto dto);
    Task<bool> DeleteCpostalAsync(long idt);

    // DESTINOS
    Task<List<SeurDestinoDto>> GetDestinosAsync(string? search, int page = 1, int pageSize = 100);
    Task<int> CountDestinosAsync(string? search);
    Task<SeurDestinoDto?> GetDestinoAsync(long idt);
    Task<SeurDestinoDto> CreateDestinoAsync(SaveDestinoDto dto);
    Task<SeurDestinoDto?> UpdateDestinoAsync(long idt, SaveDestinoDto dto);
    Task<bool> DeleteDestinoAsync(long idt);

    // PRODUCT
    Task<List<SeurProductDto>> GetProductsAsync(string? search);
    Task<SeurProductDto?> GetProductAsync(long idt);
    Task<SeurProductDto> CreateProductAsync(SaveProductDto dto);
    Task<SeurProductDto?> UpdateProductAsync(long idt, SaveProductDto dto);
    Task<bool> DeleteProductAsync(long idt);

    // SERVICE
    Task<List<SeurServiceDto>> GetServicesAsync(string? search);
    Task<SeurServiceDto?> GetServiceAsync(long idt);
    Task<SeurServiceDto> CreateServiceAsync(SaveServiceDto dto);
    Task<SeurServiceDto?> UpdateServiceAsync(long idt, SaveServiceDto dto);
    Task<bool> DeleteServiceAsync(long idt);

    // CWENT_NUM
    Task<List<CwentNumDto>> GetCwentsAsync(string? search, int page = 1, int pageSize = 100);
    Task<int> CountCwentsAsync(string? search);
    Task<CwentNumDto?> GetCwentAsync(string account);
    Task<CwentNumDto> CreateCwentAsync(CreateCwentNumDto dto);
    Task<CwentNumDto?> UpdateCwentAsync(string account, SaveCwentNumDto dto);
    Task<bool> DeleteCwentAsync(string account);
}

public class SeurTabelasService : ISeurTabelasService
{
    private readonly ApplicationDbContext _context;

    public SeurTabelasService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ---- CPOSTAL ----

    public async Task<List<SeurCpostalDto>> GetCpostaisAsync(string? search, int page = 1, int pageSize = 100)
    {
        var q = _context.SeurCpostais.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Postcode.Contains(s) || (x.Population != null && x.Population.ToUpper().Contains(s)));
        }
        return await q.OrderBy(x => x.Postcode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => ToCpostalDto(x)).ToListAsync();
    }

    public async Task<int> CountCpostaisAsync(string? search)
    {
        var q = _context.SeurCpostais.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Postcode.Contains(s) || (x.Population != null && x.Population.ToUpper().Contains(s)));
        }
        return await q.CountAsync();
    }

    public async Task<SeurCpostalDto?> GetCpostalAsync(long idt)
    {
        var x = await _context.SeurCpostais.AsNoTracking().FirstOrDefaultAsync(c => c.Idt == idt);
        return x == null ? null : ToCpostalDto(x);
    }

    public async Task<SeurCpostalDto> CreateCpostalAsync(SaveCpostalDto dto)
    {
        var maxIdt = await _context.SeurCpostais.MaxAsync(x => (long?)x.Idt) ?? 0;
        var entity = new SeurCpostal
        {
            Idt = maxIdt + 1,
            Postcode = dto.Postcode,
            Population = dto.Population,
            Country = dto.Country,
            DestFranchise = dto.DestFranchise,
            Plataform = dto.Plataform,
            DtCriacao = DateTime.Now,
            DtAlteracao = DateTime.Now,
            Apagado = "N"
        };
        _context.SeurCpostais.Add(entity);
        await _context.SaveChangesAsync();
        return ToCpostalDto(entity);
    }

    public async Task<SeurCpostalDto?> UpdateCpostalAsync(long idt, SaveCpostalDto dto)
    {
        var entity = await _context.SeurCpostais.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return null;
        entity.Postcode = dto.Postcode;
        entity.Population = dto.Population;
        entity.Country = dto.Country;
        entity.DestFranchise = dto.DestFranchise;
        entity.Plataform = dto.Plataform;
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return ToCpostalDto(entity);
    }

    public async Task<bool> DeleteCpostalAsync(long idt)
    {
        var entity = await _context.SeurCpostais.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return false;
        entity.Apagado = "S";
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    private static SeurCpostalDto ToCpostalDto(SeurCpostal x) => new()
    {
        Idt = x.Idt, Postcode = x.Postcode, Population = x.Population,
        Country = x.Country, DestFranchise = x.DestFranchise, Plataform = x.Plataform,
        DtCriacao = x.DtCriacao, DtAlteracao = x.DtAlteracao
    };

    // ---- DESTINOS ----

    public async Task<List<SeurDestinoDto>> GetDestinosAsync(string? search, int page = 1, int pageSize = 100)
    {
        var q = _context.SeurDestinos.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.DestinoCode.Contains(s) || (x.Destination != null && x.Destination.ToUpper().Contains(s)));
        }
        return await q.OrderBy(x => x.DestinoCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => ToDestinoDto(x)).ToListAsync();
    }

    public async Task<int> CountDestinosAsync(string? search)
    {
        var q = _context.SeurDestinos.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.DestinoCode.Contains(s) || (x.Destination != null && x.Destination.ToUpper().Contains(s)));
        }
        return await q.CountAsync();
    }

    public async Task<SeurDestinoDto?> GetDestinoAsync(long idt)
    {
        var x = await _context.SeurDestinos.AsNoTracking().FirstOrDefaultAsync(d => d.Idt == idt);
        return x == null ? null : ToDestinoDto(x);
    }

    public async Task<SeurDestinoDto> CreateDestinoAsync(SaveDestinoDto dto)
    {
        var maxIdt = await _context.SeurDestinos.MaxAsync(x => (long?)x.Idt) ?? 0;
        var entity = new SeurDestino
        {
            Idt = maxIdt + 1,
            DestinoCode = dto.DestinoCode,
            PlataformCode = dto.PlataformCode,
            ProductCode = dto.ProductCode,
            ServiceCode = dto.ServiceCode,
            Destination = dto.Destination,
            LoadLine = dto.LoadLine,
            TransportLine = dto.TransportLine,
            DtCriacao = DateTime.Now,
            DtAlteracao = DateTime.Now,
            Apagado = "N"
        };
        _context.SeurDestinos.Add(entity);
        await _context.SaveChangesAsync();
        return ToDestinoDto(entity);
    }

    public async Task<SeurDestinoDto?> UpdateDestinoAsync(long idt, SaveDestinoDto dto)
    {
        var entity = await _context.SeurDestinos.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return null;
        entity.DestinoCode = dto.DestinoCode;
        entity.PlataformCode = dto.PlataformCode;
        entity.ProductCode = dto.ProductCode;
        entity.ServiceCode = dto.ServiceCode;
        entity.Destination = dto.Destination;
        entity.LoadLine = dto.LoadLine;
        entity.TransportLine = dto.TransportLine;
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return ToDestinoDto(entity);
    }

    public async Task<bool> DeleteDestinoAsync(long idt)
    {
        var entity = await _context.SeurDestinos.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return false;
        entity.Apagado = "S";
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    private static SeurDestinoDto ToDestinoDto(SeurDestino x) => new()
    {
        Idt = x.Idt, DestinoCode = x.DestinoCode, PlataformCode = x.PlataformCode,
        ProductCode = x.ProductCode, ServiceCode = x.ServiceCode, Destination = x.Destination,
        LoadLine = x.LoadLine, TransportLine = x.TransportLine,
        DtCriacao = x.DtCriacao, DtAlteracao = x.DtAlteracao
    };

    // ---- PRODUCT ----

    public async Task<List<SeurProductDto>> GetProductsAsync(string? search)
    {
        var q = _context.SeurProducts.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.ProductCode.Contains(s) || (x.Product != null && x.Product.ToUpper().Contains(s)));
        }
        return await q.OrderBy(x => x.ProductCode).Select(x => ToProductDto(x)).ToListAsync();
    }

    public async Task<SeurProductDto?> GetProductAsync(long idt)
    {
        var x = await _context.SeurProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Idt == idt);
        return x == null ? null : ToProductDto(x);
    }

    public async Task<SeurProductDto> CreateProductAsync(SaveProductDto dto)
    {
        var maxIdt = await _context.SeurProducts.MaxAsync(x => (long?)x.Idt) ?? 0;
        var entity = new SeurProduct
        {
            Idt = maxIdt + 1,
            ProductCode = dto.ProductCode,
            Product = dto.Product,
            ShortName = dto.ShortName,
            DtCriacao = DateTime.Now,
            DtAlteracao = DateTime.Now,
            Apagado = "N"
        };
        _context.SeurProducts.Add(entity);
        await _context.SaveChangesAsync();
        return ToProductDto(entity);
    }

    public async Task<SeurProductDto?> UpdateProductAsync(long idt, SaveProductDto dto)
    {
        var entity = await _context.SeurProducts.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return null;
        entity.ProductCode = dto.ProductCode;
        entity.Product = dto.Product;
        entity.ShortName = dto.ShortName;
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return ToProductDto(entity);
    }

    public async Task<bool> DeleteProductAsync(long idt)
    {
        var entity = await _context.SeurProducts.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return false;
        entity.Apagado = "S";
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    private static SeurProductDto ToProductDto(SeurProduct x) => new()
    {
        Idt = x.Idt, ProductCode = x.ProductCode, Product = x.Product,
        ShortName = x.ShortName, DtCriacao = x.DtCriacao, DtAlteracao = x.DtAlteracao
    };

    // ---- SERVICE ----

    public async Task<List<SeurServiceDto>> GetServicesAsync(string? search)
    {
        var q = _context.SeurServices.AsNoTracking().Where(x => x.Apagado == "N");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.ServiceCode.Contains(s) || (x.Service != null && x.Service.ToUpper().Contains(s)));
        }
        return await q.OrderBy(x => x.ServiceCode).Select(x => ToServiceDto(x)).ToListAsync();
    }

    public async Task<SeurServiceDto?> GetServiceAsync(long idt)
    {
        var x = await _context.SeurServices.AsNoTracking().FirstOrDefaultAsync(s => s.Idt == idt);
        return x == null ? null : ToServiceDto(x);
    }

    public async Task<SeurServiceDto> CreateServiceAsync(SaveServiceDto dto)
    {
        var maxIdt = await _context.SeurServices.MaxAsync(x => (long?)x.Idt) ?? 0;
        var entity = new SeurService
        {
            Idt = maxIdt + 1,
            ServiceCode = dto.ServiceCode,
            Service = dto.Service,
            ShortName = dto.ShortName,
            DtCriacao = DateTime.Now,
            DtAlteracao = DateTime.Now,
            Apagado = "N"
        };
        _context.SeurServices.Add(entity);
        await _context.SaveChangesAsync();
        return ToServiceDto(entity);
    }

    public async Task<SeurServiceDto?> UpdateServiceAsync(long idt, SaveServiceDto dto)
    {
        var entity = await _context.SeurServices.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return null;
        entity.ServiceCode = dto.ServiceCode;
        entity.Service = dto.Service;
        entity.ShortName = dto.ShortName;
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return ToServiceDto(entity);
    }

    public async Task<bool> DeleteServiceAsync(long idt)
    {
        var entity = await _context.SeurServices.FirstOrDefaultAsync(x => x.Idt == idt);
        if (entity == null) return false;
        entity.Apagado = "S";
        entity.DtAlteracao = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    private static SeurServiceDto ToServiceDto(SeurService x) => new()
    {
        Idt = x.Idt, ServiceCode = x.ServiceCode, Service = x.Service,
        ShortName = x.ShortName, DtCriacao = x.DtCriacao, DtAlteracao = x.DtAlteracao
    };

    // ---- CWENT_NUM ----

    public async Task<List<CwentNumDto>> GetCwentsAsync(string? search, int page = 1, int pageSize = 100)
    {
        var q = _context.CwentNums.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Account.Contains(s)
                || (x.BicsUser != null && x.BicsUser.ToUpper().Contains(s))
                || (x.DescAux != null && x.DescAux.ToUpper().Contains(s)));
        }
        return await q.OrderBy(x => x.Account)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => ToCwentDto(x)).ToListAsync();
    }

    public async Task<int> CountCwentsAsync(string? search)
    {
        var q = _context.CwentNums.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            q = q.Where(x => x.Account.Contains(s)
                || (x.BicsUser != null && x.BicsUser.ToUpper().Contains(s))
                || (x.DescAux != null && x.DescAux.ToUpper().Contains(s)));
        }
        return await q.CountAsync();
    }

    public async Task<CwentNumDto?> GetCwentAsync(string account)
    {
        var x = await _context.CwentNums.AsNoTracking().FirstOrDefaultAsync(c => c.Account == account);
        return x == null ? null : ToCwentDto(x);
    }

    public async Task<CwentNumDto> CreateCwentAsync(CreateCwentNumDto dto)
    {
        if (await _context.CwentNums.FirstOrDefaultAsync(x => x.Account == dto.Account) != null)
            throw new InvalidOperationException("Conta já existe");

        var entity = MapCwent(new CwentNum { Account = dto.Account }, dto);
        _context.CwentNums.Add(entity);
        await _context.SaveChangesAsync();
        return ToCwentDto(entity);
    }

    public async Task<CwentNumDto?> UpdateCwentAsync(string account, SaveCwentNumDto dto)
    {
        var entity = await _context.CwentNums.FirstOrDefaultAsync(x => x.Account == account);
        if (entity == null) return null;
        MapCwent(entity, dto);
        await _context.SaveChangesAsync();
        return ToCwentDto(entity);
    }

    public async Task<bool> DeleteCwentAsync(string account)
    {
        var entity = await _context.CwentNums.FirstOrDefaultAsync(x => x.Account == account);
        if (entity == null) return false;
        _context.CwentNums.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private static CwentNum MapCwent(CwentNum e, SaveCwentNumDto dto)
    {
        e.BicNumber = dto.BicNumber;
        e.ShEmail2Dest = dto.ShEmail2Dest; e.ShReady2Collect = dto.ShReady2Collect;
        e.Cp4Allow = dto.Cp4Allow; e.ServiceRcSeqNumber = dto.ServiceRcSeqNumber;
        e.AgregateBilling = dto.AgregateBilling; e.GrAssinada = dto.GrAssinada;
        e.Weigth0Allow = dto.Weigth0Allow; e.LabelType = dto.LabelType;
        e.ServiceB2c = dto.ServiceB2c; e.ServFrio = dto.ServFrio;
        e.ServAux = dto.ServAux; e.DescAux = dto.DescAux;
        e.BicsUser = dto.BicsUser; e.BicsPwd = dto.BicsPwd;
        e.BicsCi = dto.BicsCi; e.BicsNif = dto.BicsNif;
        e.BicsCcc = dto.BicsCcc; e.BicsPraca = dto.BicsPraca;
        e.BicsCcc2 = dto.BicsCcc2; e.BicsUsr2 = dto.BicsUsr2;
        e.BicsServ = dto.BicsServ; e.BicsProd = dto.BicsProd;
        e.BicvPortal = dto.BicvPortal;
        e.BicAux1 = dto.BicAux1; e.BicAux2 = dto.BicAux2;
        e.BicAux3 = dto.BicAux3; e.BicAux4 = dto.BicAux4;
        return e;
    }

    private static CwentNumDto ToCwentDto(CwentNum x) => new()
    {
        Account = x.Account, BicNumber = x.BicNumber,
        ShEmail2Dest = x.ShEmail2Dest, ShReady2Collect = x.ShReady2Collect,
        Cp4Allow = x.Cp4Allow, ServiceRcSeqNumber = x.ServiceRcSeqNumber,
        AgregateBilling = x.AgregateBilling, GrAssinada = x.GrAssinada,
        Weigth0Allow = x.Weigth0Allow, LabelType = x.LabelType,
        ServiceB2c = x.ServiceB2c, ServFrio = x.ServFrio,
        ServAux = x.ServAux, DescAux = x.DescAux,
        BicsUser = x.BicsUser, BicsPwd = x.BicsPwd,
        BicsCi = x.BicsCi, BicsNif = x.BicsNif,
        BicsCcc = x.BicsCcc, BicsPraca = x.BicsPraca,
        BicsCcc2 = x.BicsCcc2, BicsUsr2 = x.BicsUsr2,
        BicsServ = x.BicsServ, BicsProd = x.BicsProd,
        BicvPortal = x.BicvPortal,
        BicAux1 = x.BicAux1, BicAux2 = x.BicAux2,
        BicAux3 = x.BicAux3, BicAux4 = x.BicAux4
    };
}
