using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface ISeurGuiaService
{
    Task<List<SeurGuiaListDto>> GetGuiasAsync(string? guia, string? referencia, DateTime? data, string? flagAtlas);
    Task<SeurGuiaDetailDto?> GetGuiaByIdtAsync(long idt);
    Task<bool> UpdateGuiaAsync(long idt, UpdateGuiaDto dto);
    Task<bool> UpdateFlagAtlasAsync(long idt, string flagAtlas);
    Task<List<SeurErroDto>> GetErrosByReferenciaAsync(string referencia);
    Task<List<SeurParcelDto>> GetParcelsByGuiaAsync(string guia);
    Task<List<SeurVerifyDto>> GetVerifyByGuiaAsync(string guia);
    Task<bool> UpdateVerifyFlagAsync(long idt, string verifyFlag);
    Task<SeurTotaisDto> GetTotaisByDataAsync(DateTime data);
}

public class SeurGuiaService : ISeurGuiaService
{
    private readonly ApplicationDbContext _context;

    public SeurGuiaService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static string FlagAtlasDescricao(string? flag) => flag switch
    {
        "N" => "Não Enviado",
        "Y" => "Enviado",
        "E" => "Erro",
        "X" => "Outros",
        _ => flag ?? "-"
    };

    private static string VerifyFlagDescricao(string? flag) => flag switch
    {
        "N" => "Não Verificado",
        "Y" => "Verificado",
        _ => flag ?? "-"
    };

    public async Task<List<SeurGuiaListDto>> GetGuiasAsync(string? guia, string? referencia, DateTime? data, string? flagAtlas)
    {
        var query = _context.SeurGuias.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(guia))
            query = query.Where(g => g.Guia == guia);

        if (!string.IsNullOrWhiteSpace(referencia))
            query = query.Where(g => g.Referencia == referencia);

        if (data.HasValue)
            query = query.Where(g => g.DtCriacao.Date == data.Value.Date);

        if (!string.IsNullOrWhiteSpace(flagAtlas))
            query = query.Where(g => g.FlagAtlas == flagAtlas);

        var result = await query
            .OrderByDescending(g => g.DtCriacao)
            .Select(g => new SeurGuiaListDto
            {
                Idt = g.Idt,
                Guia = g.Guia,
                Referencia = g.Referencia,
                QtdVolumes = g.QtdVolumes,
                ContaDpd = g.ContaDpd,
                DestinoNome = g.DestinoNome,
                DestinoLocalidade = g.DestinoLocalidade,
                DestinoPais = g.DestinoPais,
                Peso = g.Peso,
                Product = g.Product,
                Service = g.Service,
                FlagAtlas = g.FlagAtlas,
                FlagAtlasDescricao = null,
                DtCriacao = g.DtCriacao,
                DtAlteracao = g.DtAlteracao,
                ShipmentCode = g.ShipmentCode,
                CodeStatusAtlas = g.CodeStatusAtlas,
                DtAtlas = g.DtAtlas
            })
            .Take(500)
            .ToListAsync();

        foreach (var r in result)
            r.FlagAtlasDescricao = FlagAtlasDescricao(r.FlagAtlas);

        return result;
    }

    public async Task<SeurGuiaDetailDto?> GetGuiaByIdtAsync(long idt)
    {
        var g = await _context.SeurGuias.AsNoTracking().FirstOrDefaultAsync(x => x.Idt == idt);
        if (g == null) return null;

        return new SeurGuiaDetailDto
        {
            Idt = g.Idt,
            Guia = g.Guia,
            Referencia = g.Referencia,
            QtdVolumes = g.QtdVolumes,
            ContaDpd = g.ContaDpd,
            Peso = g.Peso,
            ValorCod = g.ValorCod,
            OrigemNome = g.OrigemNome,
            OrigemMorada = g.OrigemMorada,
            OrigemMoradaComplemento = g.OrigemMoradaComplemento,
            OrigemCodigoPostal = g.OrigemCodigoPostal,
            OrigemLocalidade = g.OrigemLocalidade,
            OrigemPais = g.OrigemPais,
            OrigemTelefone = g.OrigemTelefone,
            OrigemTelemovel = g.OrigemTelemovel,
            OrigemFax = g.OrigemFax,
            OrigemEmail = g.OrigemEmail,
            OrigemContatoNome = g.OrigemContatoNome,
            OrigemContatoTelefone = g.OrigemContatoTelefone,
            OrigemContatoTelemovel = g.OrigemContatoTelemovel,
            OrigemContatoEmail = g.OrigemContatoEmail,
            OrigemPraca = g.OrigemPraca,
            DestinoNome = g.DestinoNome,
            DestinoMorada = g.DestinoMorada,
            DestinoMoradaComplemento = g.DestinoMoradaComplemento,
            DestinoCodigoPostal = g.DestinoCodigoPostal,
            DestinoLocalidade = g.DestinoLocalidade,
            DestinoPais = g.DestinoPais,
            DestinoTelefone = g.DestinoTelefone,
            DestinoTelemovel = g.DestinoTelemovel,
            DestinoFax = g.DestinoFax,
            DestinoEmail = g.DestinoEmail,
            DestinoContatoNome = g.DestinoContatoNome,
            DestinoContatoTelefone = g.DestinoContatoTelefone,
            DestinoContatoTelemovel = g.DestinoContatoTelemovel,
            DestinoContatoEmail = g.DestinoContatoEmail,
            DestinoPraca = g.DestinoPraca,
            Destino = g.Destino,
            Obs = g.Obs,
            ObsAdd = g.ObsAdd,
            ProdutoCodigo = g.ProdutoCodigo,
            ServicoCodigo = g.ServicoCodigo,
            Product = g.Product,
            Service = g.Service,
            Cccc = g.Cccc,
            CodCentro = g.CodCentro,
            TransportLine = g.TransportLine,
            Digito = g.Digito,
            IdRange = g.IdRange,
            Range = g.Range,
            UserId = g.UserId,
            ParcelNumber = g.ParcelNumber,
            GuiaDpd = g.GuiaDpd,
            ShipmentCode = g.ShipmentCode,
            FlagAtlas = g.FlagAtlas,
            FlagAtlasDescricao = FlagAtlasDescricao(g.FlagAtlas),
            CodeStatusAtlas = g.CodeStatusAtlas,
            DtAtlas = g.DtAtlas,
            RequestAtlas = g.RequestAtlas,
            RespAtlas = g.RespAtlas,
            FlagCit = g.FlagCit,
            DtCit = g.DtCit,
            RespCit = g.RespCit,
            FlagAs400 = g.FlagAs400,
            Apagado = g.Apagado,
            VerifyFlag = g.VerifyFlag,
            VerifyResp = g.VerifyResp,
            VerifyInc = g.VerifyInc,
            VerifyDatahoraInc = g.VerifyDatahoraInc,
            VerifyDatahoraUpd = g.VerifyDatahoraUpd,
            DataVerifyTrace = g.DataVerifyTrace,
            VerifyTrace = g.VerifyTrace,
            DtCriacao = g.DtCriacao,
            DtAlteracao = g.DtAlteracao
        };
    }

    public async Task<bool> UpdateGuiaAsync(long idt, UpdateGuiaDto dto)
    {
        var guia = await _context.SeurGuias.FindAsync(idt);
        if (guia == null) return false;

        // Para campos string: ?? mantém o valor existente se o DTO enviar null
        // (protege colunas NOT NULL do Oracle quando o frontend não enviou valor)
        guia.Guia = dto.Guia ?? guia.Guia;
        guia.Referencia = dto.Referencia ?? guia.Referencia;
        guia.QtdVolumes = dto.QtdVolumes;
        guia.ContaDpd = dto.ContaDpd ?? guia.ContaDpd;
        guia.Peso = dto.Peso;
        guia.ValorCod = dto.ValorCod;
        guia.OrigemNome = dto.OrigemNome ?? guia.OrigemNome;
        guia.OrigemMorada = dto.OrigemMorada ?? guia.OrigemMorada;
        guia.OrigemMoradaComplemento = dto.OrigemMoradaComplemento ?? guia.OrigemMoradaComplemento;
        guia.OrigemCodigoPostal = dto.OrigemCodigoPostal ?? guia.OrigemCodigoPostal;
        guia.OrigemLocalidade = dto.OrigemLocalidade ?? guia.OrigemLocalidade;
        guia.OrigemPais = dto.OrigemPais ?? guia.OrigemPais;
        guia.OrigemTelefone = dto.OrigemTelefone ?? guia.OrigemTelefone;
        guia.OrigemTelemovel = dto.OrigemTelemovel ?? guia.OrigemTelemovel;
        guia.OrigemFax = dto.OrigemFax ?? guia.OrigemFax;
        guia.OrigemEmail = dto.OrigemEmail ?? guia.OrigemEmail;
        guia.OrigemContatoNome = dto.OrigemContatoNome ?? guia.OrigemContatoNome;
        guia.OrigemContatoTelefone = dto.OrigemContatoTelefone ?? guia.OrigemContatoTelefone;
        guia.OrigemContatoTelemovel = dto.OrigemContatoTelemovel ?? guia.OrigemContatoTelemovel;
        guia.OrigemContatoEmail = dto.OrigemContatoEmail ?? guia.OrigemContatoEmail;
        guia.OrigemPraca = dto.OrigemPraca ?? guia.OrigemPraca;
        guia.DestinoNome = dto.DestinoNome ?? guia.DestinoNome;
        guia.DestinoMorada = dto.DestinoMorada ?? guia.DestinoMorada;
        guia.DestinoMoradaComplemento = dto.DestinoMoradaComplemento ?? guia.DestinoMoradaComplemento;
        guia.DestinoCodigoPostal = dto.DestinoCodigoPostal ?? guia.DestinoCodigoPostal;
        guia.DestinoLocalidade = dto.DestinoLocalidade ?? guia.DestinoLocalidade;
        guia.DestinoPais = dto.DestinoPais ?? guia.DestinoPais;
        guia.DestinoTelefone = dto.DestinoTelefone ?? guia.DestinoTelefone;
        guia.DestinoTelemovel = dto.DestinoTelemovel ?? guia.DestinoTelemovel;
        guia.DestinoFax = dto.DestinoFax ?? guia.DestinoFax;
        guia.DestinoEmail = dto.DestinoEmail ?? guia.DestinoEmail;
        guia.DestinoContatoNome = dto.DestinoContatoNome ?? guia.DestinoContatoNome;
        guia.DestinoContatoTelefone = dto.DestinoContatoTelefone ?? guia.DestinoContatoTelefone;
        guia.DestinoContatoTelemovel = dto.DestinoContatoTelemovel ?? guia.DestinoContatoTelemovel;
        guia.DestinoContatoEmail = dto.DestinoContatoEmail ?? guia.DestinoContatoEmail;
        guia.DestinoPraca = dto.DestinoPraca ?? guia.DestinoPraca;
        guia.Destino = dto.Destino ?? guia.Destino;
        guia.Obs = dto.Obs ?? guia.Obs;
        guia.ObsAdd = dto.ObsAdd ?? guia.ObsAdd;
        guia.ProdutoCodigo = dto.ProdutoCodigo ?? guia.ProdutoCodigo;
        guia.ServicoCodigo = dto.ServicoCodigo ?? guia.ServicoCodigo;
        guia.Product = dto.Product ?? guia.Product;
        guia.Service = dto.Service ?? guia.Service;
        guia.Cccc = dto.Cccc ?? guia.Cccc;
        guia.CodCentro = dto.CodCentro ?? guia.CodCentro;
        guia.TransportLine = dto.TransportLine ?? guia.TransportLine;
        guia.Digito = dto.Digito ?? guia.Digito;
        guia.IdRange = dto.IdRange;
        guia.Range = dto.Range ?? guia.Range;
        guia.UserId = dto.UserId;
        guia.ParcelNumber = dto.ParcelNumber ?? guia.ParcelNumber;
        guia.GuiaDpd = dto.GuiaDpd ?? guia.GuiaDpd;
        guia.ShipmentCode = dto.ShipmentCode ?? guia.ShipmentCode;
        guia.FlagAtlas = dto.FlagAtlas ?? guia.FlagAtlas;
        guia.CodeStatusAtlas = dto.CodeStatusAtlas ?? guia.CodeStatusAtlas;
        guia.DtAtlas = dto.DtAtlas;
        guia.RequestAtlas = dto.RequestAtlas ?? guia.RequestAtlas;
        guia.RespAtlas = dto.RespAtlas ?? guia.RespAtlas;
        guia.FlagCit = dto.FlagCit ?? guia.FlagCit;
        guia.DtCit = dto.DtCit;
        guia.RespCit = dto.RespCit ?? guia.RespCit;
        guia.FlagAs400 = dto.FlagAs400 ?? guia.FlagAs400;
        guia.Apagado = dto.Apagado ?? guia.Apagado;
        guia.VerifyFlag = dto.VerifyFlag ?? guia.VerifyFlag;
        guia.VerifyResp = dto.VerifyResp ?? guia.VerifyResp;
        guia.VerifyInc = dto.VerifyInc ?? guia.VerifyInc;
        guia.VerifyDatahoraInc = dto.VerifyDatahoraInc ?? guia.VerifyDatahoraInc;
        guia.VerifyDatahoraUpd = dto.VerifyDatahoraUpd;
        guia.DataVerifyTrace = dto.DataVerifyTrace;
        guia.VerifyTrace = dto.VerifyTrace ?? guia.VerifyTrace;
        guia.DtAlteracao = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateFlagAtlasAsync(long idt, string flagAtlas)
    {
        var guia = await _context.SeurGuias.FindAsync(idt);
        if (guia == null) return false;

        guia.FlagAtlas = flagAtlas;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SeurErroDto>> GetErrosByReferenciaAsync(string referencia)
    {
        return await _context.SeurErros
            .AsNoTracking()
            .Where(e => e.Referencia == referencia)
            .OrderByDescending(e => e.DatahoraInsert)
            .Select(e => new SeurErroDto
            {
                Idt = e.Idt,
                Ecb = e.Ecb,
                Referencia = e.Referencia,
                Title = e.Title,
                Status = e.Status,
                Detail = e.Detail,
                DatahoraInsert = e.DatahoraInsert
            })
            .ToListAsync();
    }

    public async Task<List<SeurParcelDto>> GetParcelsByGuiaAsync(string guia)
    {
        return await _context.SeurParcels
            .AsNoTracking()
            .Where(p => p.Ecbs == guia)
            .OrderByDescending(p => p.DatahoraInsert)
            .Select(p => new SeurParcelDto
            {
                Idt = p.Idt,
                Ecbs = p.Ecbs,
                ParcelNumbers = p.ParcelNumbers,
                DatahoraInsert = p.DatahoraInsert,
                Flag = p.Flag,
                ShipmentCode = p.ShipmentCode
            })
            .ToListAsync();
    }

    public async Task<List<SeurVerifyDto>> GetVerifyByGuiaAsync(string guia)
    {
        return await _context.SeurVerifies
            .AsNoTracking()
            .Where(v => v.Guia == guia)
            .OrderByDescending(v => v.DatahoraInsert)
            .Select(v => new SeurVerifyDto
            {
                Idt = v.Idt,
                Guia = v.Guia,
                Inc = v.Inc,
                Dat = v.Dat,
                Hor = v.Hor,
                DatahoraInsert = v.DatahoraInsert,
                VerifyFlag = v.VerifyFlag,
                VerifyFlagDescricao = null,
                VerifyResposta = v.VerifyResposta,
                VerifyData = v.VerifyData
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateVerifyFlagAsync(long idt, string verifyFlag)
    {
        var verify = await _context.SeurVerifies.FindAsync(idt);
        if (verify == null) return false;

        verify.VerifyFlag = verifyFlag;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SeurTotaisDto> GetTotaisByDataAsync(DateTime data)
    {
        var inicio = data.Date;
        var fim = inicio.AddDays(1);

        var flags = await _context.SeurGuias
            .AsNoTracking()
            .Where(g => g.DtCriacao >= inicio && g.DtCriacao < fim)
            .Select(g => g.FlagAtlas)
            .ToListAsync();

        return new SeurTotaisDto
        {
            Total = flags.Count,
            Enviado = flags.Count(f => f == "Y"),
            Erro = flags.Count(f => f == "E"),
            NaoEnviado = flags.Count(f => f == "N"),
            Outros = flags.Count(f => f != "Y" && f != "E" && f != "N")
        };
    }
}
