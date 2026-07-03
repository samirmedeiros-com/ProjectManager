using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<SeurUser> SeurUsers { get; set; }

    // Tabelas Oracle existentes (read/update, não geridas por migrations)
    public DbSet<SeurGuia> SeurGuias { get; set; }
    public DbSet<SeurErro> SeurErros { get; set; }
    public DbSet<SeurParcel> SeurParcels { get; set; }
    public DbSet<SeurVerify> SeurVerifies { get; set; }
    public DbSet<SeurCpostal> SeurCpostais { get; set; }
    public DbSet<SeurDestino> SeurDestinos { get; set; }
    public DbSet<SeurProduct> SeurProducts { get; set; }
    public DbSet<SeurService> SeurServices { get; set; }
    public DbSet<CwentNum> CwentNums { get; set; }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<ProjectStatusHistory> ProjectStatusHistories { get; set; }
    public DbSet<ProjectManagerHistory> ProjectManagerHistories { get; set; }
    public DbSet<ProjectOwnerHistory> ProjectOwnerHistories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Setor> Setores { get; set; }
    public DbSet<UserSetor> UserSetores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar campos booleanos para Oracle (converter bool para NUMBER(1) com valores 0/1)
        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<ProjectMember>()
            .Property(pm => pm.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<Setor>()
            .Property(s => s.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<ProjectTask>()
            .Property(pt => pt.Progress)
            .HasPrecision(5, 2);

        // Índice único para Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<SeurUser>()
            .Property(u => u.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<SeurUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectMembers)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMembers)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectStatusHistory>()
            .HasOne(sh => sh.Project)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(sh => sh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectManagerHistory>()
            .HasOne(mh => mh.Project)
            .WithMany(p => p.ManagerHistory)
            .HasForeignKey(mh => mh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectOwnerHistory>()
            .HasOne(oh => oh.Project)
            .WithMany(p => p.OwnerHistory)
            .HasForeignKey(oh => oh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSetores)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasOne(us => us.Setor)
            .WithMany(s => s.UserSetores)
            .HasForeignKey(us => us.SetorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasIndex(us => new { us.UserId, us.SetorId })
            .IsUnique();

        // --- Tabelas Oracle existentes (excluídas de migrations) ---
        modelBuilder.Entity<SeurGuia>(e =>
        {
            e.ToTable("SEUR_GUIA", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(g => g.Idt);
            e.Property(g => g.Idt).HasColumnName("IDT");
            e.Property(g => g.ContaDpd).HasColumnName("CONTADPD");
            e.Property(g => g.Guia).HasColumnName("GUIA");
            e.Property(g => g.Referencia).HasColumnName("REFERENCIA");
            e.Property(g => g.QtdVolumes).HasColumnName("QTDVOLUMES");
            e.Property(g => g.Peso).HasColumnName("PESO");
            e.Property(g => g.ValorCod).HasColumnName("VALORCOD");
            e.Property(g => g.OrigemNome).HasColumnName("ORIGEMNOME");
            e.Property(g => g.OrigemMorada).HasColumnName("ORIGEMMORADA");
            e.Property(g => g.OrigemMoradaComplemento).HasColumnName("ORIGEMMORADACOMPLEMENTO");
            e.Property(g => g.OrigemCodigoPostal).HasColumnName("ORIGEMCODIGOPOSTAL");
            e.Property(g => g.OrigemLocalidade).HasColumnName("ORIGEMLOCALIDADE");
            e.Property(g => g.OrigemPais).HasColumnName("ORIGEMPAIS");
            e.Property(g => g.OrigemTelefone).HasColumnName("ORIGEMTELEFONE");
            e.Property(g => g.OrigemTelemovel).HasColumnName("ORIGEMTELEMOVEL");
            e.Property(g => g.OrigemFax).HasColumnName("ORIGEMFAX");
            e.Property(g => g.OrigemEmail).HasColumnName("ORIGEMEMAIL");
            e.Property(g => g.OrigemContatoNome).HasColumnName("ORIGEMCONTATONOME");
            e.Property(g => g.OrigemContatoTelefone).HasColumnName("ORIGEMCONTATOTELEFONE");
            e.Property(g => g.OrigemContatoTelemovel).HasColumnName("ORIGEMCONTATOTELEMOVEL");
            e.Property(g => g.OrigemContatoEmail).HasColumnName("ORIGEMCONTATOEMAIL");
            e.Property(g => g.OrigemPraca).HasColumnName("ORIGEMPRACA");
            e.Property(g => g.DestinoNome).HasColumnName("DESTINONOME");
            e.Property(g => g.DestinoMorada).HasColumnName("DESTINOMORADA");
            e.Property(g => g.DestinoMoradaComplemento).HasColumnName("DESTINOMORADACOMPLEMENTO");
            e.Property(g => g.DestinoCodigoPostal).HasColumnName("DESTINOCODIGOPOSTAL");
            e.Property(g => g.DestinoLocalidade).HasColumnName("DESTINOLOCALIDADE");
            e.Property(g => g.DestinoPais).HasColumnName("DESTINOPAIS");
            e.Property(g => g.DestinoTelefone).HasColumnName("DESTINOTELEFONE");
            e.Property(g => g.DestinoTelemovel).HasColumnName("DESTINOTELEMOVEL");
            e.Property(g => g.DestinoFax).HasColumnName("DESTINOFAX");
            e.Property(g => g.DestinoEmail).HasColumnName("DESTINOEMAIL");
            e.Property(g => g.DestinoContatoNome).HasColumnName("DESTINOCONTATONOME");
            e.Property(g => g.DestinoContatoTelefone).HasColumnName("DESTINOCONTATOTELEFONE");
            e.Property(g => g.DestinoContatoTelemovel).HasColumnName("DESTINOCONTATOTELEMOVEL");
            e.Property(g => g.DestinoContatoEmail).HasColumnName("DESTINOCONTATOEMAIL");
            e.Property(g => g.DestinoPraca).HasColumnName("DESTINOPRACA");
            e.Property(g => g.Destino).HasColumnName("DESTINO");
            e.Property(g => g.Obs).HasColumnName("OBS");
            e.Property(g => g.ObsAdd).HasColumnName("OBSADD");
            e.Property(g => g.ProdutoCodigo).HasColumnName("PRODUTOCODIGO");
            e.Property(g => g.ServicoCodigo).HasColumnName("SERVICOCODIGO");
            e.Property(g => g.TransportLine).HasColumnName("TRANSPORTLINE");
            e.Property(g => g.Cccc).HasColumnName("CCCC");
            e.Property(g => g.Digito).HasColumnName("DIGITO");
            e.Property(g => g.IdRange).HasColumnName("IDRANGE");
            e.Property(g => g.Range).HasColumnName("RANGE");
            e.Property(g => g.CodCentro).HasColumnName("COD_CENTRO");
            e.Property(g => g.Product).HasColumnName("PRODUCT");
            e.Property(g => g.Service).HasColumnName("SERVICE");
            e.Property(g => g.ParcelNumber).HasColumnName("PARCELNUMBER");
            e.Property(g => g.GuiaDpd).HasColumnName("GUIADPD");
            e.Property(g => g.UserId).HasColumnName("USERID");
            e.Property(g => g.FlagCit).HasColumnName("FLAGCIT");
            e.Property(g => g.DtCit).HasColumnName("DT_CIT");
            e.Property(g => g.RespCit).HasColumnName("RESPCIT");
            e.Property(g => g.DtCriacao).HasColumnName("DT_CRIACAO");
            e.Property(g => g.DtAlteracao).HasColumnName("DT_ALTERACAO");
            e.Property(g => g.Apagado).HasColumnName("APAGADO");
            e.Property(g => g.FlagAs400).HasColumnName("FLAG_AS400");
            e.Property(g => g.VerifyFlag).HasColumnName("VERIFY_FLAG");
            e.Property(g => g.VerifyInc).HasColumnName("VERIFY_INC");
            e.Property(g => g.VerifyDatahoraInc).HasColumnName("VERIFY_DATAHORA_INC");
            e.Property(g => g.VerifyDatahoraUpd).HasColumnName("VERIFY_DATAHORA_UPD");
            e.Property(g => g.VerifyResp).HasColumnName("VERIFY_RESP");
            e.Property(g => g.DataVerifyTrace).HasColumnName("DATAVERIFYTRACE");
            e.Property(g => g.VerifyTrace).HasColumnName("VERIFYTRACE");
            e.Property(g => g.FlagAtlas).HasColumnName("FLAGATLAS");
            e.Property(g => g.RespAtlas).HasColumnName("RESP_ATLAS");
            e.Property(g => g.DtAtlas).HasColumnName("DT_ATLAS");
            e.Property(g => g.CodeStatusAtlas).HasColumnName("CODESTATUSATLAS");
            e.Property(g => g.RequestAtlas).HasColumnName("REQUESTATLAS");
            e.Property(g => g.ShipmentCode).HasColumnName("SHIPMENTCODE");
        });

        modelBuilder.Entity<SeurErro>(e =>
        {
            e.ToTable("SEUR_ERRO", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.Ecb).HasColumnName("ECB");
            e.Property(x => x.Referencia).HasColumnName("REFERENCIA");
            e.Property(x => x.Title).HasColumnName("TITLE");
            e.Property(x => x.Status).HasColumnName("STATUS");
            e.Property(x => x.Detail).HasColumnName("DETAIL");
            e.Property(x => x.DatahoraInsert).HasColumnName("DATAHORA_INSERT");
        });

        modelBuilder.Entity<SeurParcel>(e =>
        {
            e.ToTable("SEUR_PARCEL", "CHRONO_WEB", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.Ecbs).HasColumnName("ECBS");
            e.Property(x => x.ParcelNumbers).HasColumnName("PARCELNUMBERS");
            e.Property(x => x.DatahoraInsert).HasColumnName("DATAHORA_INSERT");
            e.Property(x => x.Flag).HasColumnName("FLAG");
            e.Property(x => x.ShipmentCode).HasColumnName("SHIPMENTCODE");
        });

        modelBuilder.Entity<SeurVerify>(e =>
        {
            e.ToTable("VERIFY_SEUR", "CHRONO_WEB", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.Guia).HasColumnName("GUIA");
            e.Property(x => x.Inc).HasColumnName("INC");
            e.Property(x => x.Dat).HasColumnName("DAT");
            e.Property(x => x.Hor).HasColumnName("HOR");
            e.Property(x => x.DatahoraInsert).HasColumnName("DATAHORA_INSERT");
            e.Property(x => x.VerifyFlag).HasColumnName("VERIFY_FLAG");
            e.Property(x => x.VerifyResposta).HasColumnName("VERIFY_RESPOSTA");
            e.Property(x => x.VerifyData).HasColumnName("VERIFY_DATA");
        });

        modelBuilder.Entity<SeurCpostal>(e =>
        {
            e.ToTable("SEUR_CPOSTAL", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.Postcode).HasColumnName("POSTCODE");
            e.Property(x => x.Population).HasColumnName("POPULATION");
            e.Property(x => x.Country).HasColumnName("COUNTRY");
            e.Property(x => x.DestFranchise).HasColumnName("DEST_FRANCHISE");
            e.Property(x => x.Plataform).HasColumnName("PLATAFORM");
            e.Property(x => x.DtCriacao).HasColumnName("DT_CRIACAO");
            e.Property(x => x.DtAlteracao).HasColumnName("DT_ALTERACAO");
            e.Property(x => x.Apagado).HasColumnName("APAGADO");
        });

        modelBuilder.Entity<SeurDestino>(e =>
        {
            e.ToTable("SEUR_DESTINOS", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.DestinoCode).HasColumnName("DESTINOCODE");
            e.Property(x => x.PlataformCode).HasColumnName("PLATAFORMCODE");
            e.Property(x => x.ProductCode).HasColumnName("PRODUCTCODE");
            e.Property(x => x.ServiceCode).HasColumnName("SERVICECODE");
            e.Property(x => x.Destination).HasColumnName("DESTINATION");
            e.Property(x => x.LoadLine).HasColumnName("LOADLINE");
            e.Property(x => x.TransportLine).HasColumnName("TRANSPORTLINE");
            e.Property(x => x.DtCriacao).HasColumnName("DT_CRIACAO");
            e.Property(x => x.DtAlteracao).HasColumnName("DT_ALTERACAO");
            e.Property(x => x.Apagado).HasColumnName("APAGADO");
        });

        modelBuilder.Entity<SeurProduct>(e =>
        {
            e.ToTable("SEUR_PRODUCT", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.ProductCode).HasColumnName("PRODUCTCODE");
            e.Property(x => x.Product).HasColumnName("PRODUCT");
            e.Property(x => x.ShortName).HasColumnName("SHORTNAME");
            e.Property(x => x.DtCriacao).HasColumnName("DT_CRIACAO");
            e.Property(x => x.DtAlteracao).HasColumnName("DT_ALTERACAO");
            e.Property(x => x.Apagado).HasColumnName("APAGADO");
        });

        modelBuilder.Entity<SeurService>(e =>
        {
            e.ToTable("SEUR_SERVICE", "SEUR", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Idt);
            e.Property(x => x.Idt).HasColumnName("IDT");
            e.Property(x => x.ServiceCode).HasColumnName("SERVICECODE");
            e.Property(x => x.Service).HasColumnName("SERVICE");
            e.Property(x => x.ShortName).HasColumnName("SHORTNAME");
            e.Property(x => x.DtCriacao).HasColumnName("DT_CRIACAO");
            e.Property(x => x.DtAlteracao).HasColumnName("DT_ALTERACAO");
            e.Property(x => x.Apagado).HasColumnName("APAGADO");
        });

        modelBuilder.Entity<CwentNum>(e =>
        {
            e.ToTable("CWENT_NUM", "CHRONO_WEB", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Account);
            e.Property(x => x.Account).HasColumnName("ACCOUNT");
            e.Property(x => x.BicNumber).HasColumnName("BIC_NUMBER");
            e.Property(x => x.ShEmail2Dest).HasColumnName("SH_EMAIL2DEST");
            e.Property(x => x.ShReady2Collect).HasColumnName("SH_READY2COLLECT");
            e.Property(x => x.Cp4Allow).HasColumnName("CP4_ALLOW");
            e.Property(x => x.ServiceRcSeqNumber).HasColumnName("SERVICE_RC_SEQ_NUMBER");
            e.Property(x => x.AgregateBilling).HasColumnName("AGREGATE_BILLING");
            e.Property(x => x.GrAssinada).HasColumnName("GR_ASSINADA");
            e.Property(x => x.Weigth0Allow).HasColumnName("WEIGTH_0_ALLOW");
            e.Property(x => x.LabelType).HasColumnName("LABEL_TYPE");
            e.Property(x => x.ServiceB2c).HasColumnName("SERVICE_B2C");
            e.Property(x => x.ServFrio).HasColumnName("SERV_FRIO");
            e.Property(x => x.ServAux).HasColumnName("SERV_AUX");
            e.Property(x => x.DescAux).HasColumnName("DESC_AUX");
            e.Property(x => x.BicsUser).HasColumnName("BICSUSER");
            e.Property(x => x.BicsPwd).HasColumnName("BICSPWD");
            e.Property(x => x.BicsCi).HasColumnName("BICSCI");
            e.Property(x => x.BicsNif).HasColumnName("BICSNIF");
            e.Property(x => x.BicsCcc).HasColumnName("BICSCCC");
            e.Property(x => x.BicsPraca).HasColumnName("BICSPRACA");
            e.Property(x => x.BicsCcc2).HasColumnName("BICSCCC2");
            e.Property(x => x.BicsUsr2).HasColumnName("BICSUSR2");
            e.Property(x => x.BicsServ).HasColumnName("BICSSERV");
            e.Property(x => x.BicsProd).HasColumnName("BICSPROD");
            e.Property(x => x.BicvPortal).HasColumnName("BICVPORTAL");
            e.Property(x => x.BicAux1).HasColumnName("BICAUX1");
            e.Property(x => x.BicAux2).HasColumnName("BICAUX2");
            e.Property(x => x.BicAux3).HasColumnName("BICAUX3");
            e.Property(x => x.BicAux4).HasColumnName("BICAUX4");
        });
    }
}
