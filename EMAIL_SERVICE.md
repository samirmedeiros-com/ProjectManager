# 📧 Email Service - Project Manager

Guia completo para usar o serviço de envio de emails via SMTP no ProjectManager.

## 📋 Overview

O serviço de email permite enviar:
- ✅ Emails simples com texto
- ✅ Emails em HTML formatado
- ✅ Emails com múltiplos destinatários (To, CC, BCC)
- ✅ Emails com anexos
- ✅ Envio em lote
- ✅ Teste de configuração SMTP

## 🔧 Configuração

### 1. Configurar SMTP

Edite `appsettings.json`:

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "seu-email@gmail.com",
  "Password": "sua-senha-app",
  "FromEmail": "seu-email@gmail.com",
  "FromName": "Project Manager",
  "EnableSsl": true,
  "Timeout": 30000
}
```

### 2. Variáveis de Ambiente (Produção)

```bash
# No servidor ou CI/CD
export SMTPSETTINGS__HOST=smtp.gmail.com
export SMTPSETTINGS__PORT=587
export SMTPSETTINGS__USERNAME=seu-email@gmail.com
export SMTPSETTINGS__PASSWORD=sua-senha-app
export SMTPSETTINGS__FROMEMAIL=seu-email@gmail.com
export SMTPSETTINGS__FROMNAME="Project Manager"
```

## 🔌 Provedores SMTP Populares

### Gmail

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "seu-email@gmail.com",
  "Password": "sua-senha-app-gerada",
  "FromEmail": "seu-email@gmail.com",
  "EnableSsl": true
}
```

**Passos:**
1. Ativar autenticação de 2 fatores
2. Gerar senha de app: [Google App Passwords](https://myaccount.google.com/apppasswords)
3. Usar a senha gerada no campo `Password`

### Microsoft Exchange

```json
"SmtpSettings": {
  "Host": "smtp.office365.com",
  "Port": 587,
  "Username": "seu-email@empresa.com",
  "Password": "sua-senha",
  "FromEmail": "seu-email@empresa.com",
  "EnableSsl": true
}
```

### SendGrid

```json
"SmtpSettings": {
  "Host": "smtp.sendgrid.net",
  "Port": 587,
  "Username": "apikey",
  "Password": "sua-chave-api-sendgrid",
  "FromEmail": "seu-email@dominio.com",
  "EnableSsl": true
}
```

## 🚀 Uso da API

### 1. Enviar Email Simples

```bash
curl -X POST http://localhost:5000/api/email/send \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "to": "destinatario@example.com",
    "subject": "Olá",
    "body": "Este é um email de teste"
  }'
```

### 2. Enviar Email com HTML

```bash
curl -X POST http://localhost:5000/api/email/send \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "to": "destinatario@example.com",
    "subject": "Bem-vindo",
    "body": "Texto alternativo para clientes sem suporte a HTML",
    "htmlBody": "<h1>Bem-vindo!</h1><p>Este é um email em <strong>HTML</strong></p>"
  }'
```

### 3. Enviar com CC e BCC

```bash
curl -X POST http://localhost:5000/api/email/send \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "to": "principal@example.com",
    "subject": "Relatório",
    "body": "Veja o relatório em anexo",
    "ccEmails": ["copia@example.com"],
    "bccEmails": ["arquivo@example.com"]
  }'
```

### 4. Enviar em Lote

```bash
curl -X POST http://localhost:5000/api/email/send-batch \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "to": "usuario1@example.com",
      "subject": "Notificação",
      "body": "Mensagem 1"
    },
    {
      "to": "usuario2@example.com",
      "subject": "Notificação",
      "body": "Mensagem 2"
    }
  ]'
```

### 5. Testar Configuração SMTP

```bash
curl -X POST http://localhost:5000/api/email/test \
  -H "Authorization: Bearer <TOKEN>"
```

Enviar um email de teste para o email do usuário logado.

## 💻 Usar no Frontend (Angular)

Crie um serviço:

```typescript
// services/email.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EmailService {
  private apiUrl = 'http://localhost:5000/api/email';

  constructor(private http: HttpClient) {}

  sendEmail(request: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/send`, request);
  }

  sendEmailBatch(requests: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/send-batch`, requests);
  }

  testSmtp(): Observable<any> {
    return this.http.post(`${this.apiUrl}/test`, {});
  }
}
```

Usar no componente:

```typescript
// components/project-detail.component.ts
import { EmailService } from '../../services/email.service';

export class ProjectDetailComponent {
  constructor(private emailService: EmailService) {}

  notifyTeam() {
    const emailRequest = {
      to: 'team@example.com',
      subject: 'Novo projeto criado',
      htmlBody: `
        <h2>Novo Projeto: ${this.project.name}</h2>
        <p>${this.project.description}</p>
        <a href="http://localhost:4200/projects/${this.project.id}">Ver Projeto</a>
      `
    };

    this.emailService.sendEmail(emailRequest).subscribe({
      next: (response) => {
        console.log('Email enviado:', response);
      },
      error: (error) => {
        console.error('Erro ao enviar email:', error);
      }
    });
  }
}
```

## 📧 Casos de Uso

### 1. Notificar Criação de Projeto

```csharp
// ProjectService.cs
public async Task NotifyProjectCreated(Project project, string createdByEmail)
{
    var emailRequest = new EmailRequest
    {
        To = project.Manager ?? "manager@example.com",
        Subject = $"Novo Projeto: {project.Name}",
        HtmlBody = $@"
            <h2>{project.Name}</h2>
            <p>{project.Description}</p>
            <p><strong>Data de Início:</strong> {project.StartDate:dd/MM/yyyy}</p>
            <p><strong>Data de Término:</strong> {project.EndDate:dd/MM/yyyy}</p>
            <p><strong>Criado por:</strong> {createdByEmail}</p>
        "
    };

    await _emailService.SendEmailAsync(emailRequest);
}
```

### 2. Notificar Mudança de Status

```csharp
// ProjectService.cs
public async Task NotifyStatusChanged(int projectId, string newStatus)
{
    var project = await _context.Projects.FindAsync(projectId);
    var notification = new EmailRequest
    {
        To = project?.Manager ?? "manager@example.com",
        Subject = $"Status do projeto '{project?.Name}' alterado",
        HtmlBody = $@"
            <p>O status do projeto <strong>{project?.Name}</strong> foi alterado para:</p>
            <p style='font-size: 20px; color: blue;'><strong>{newStatus}</strong></p>
        "
    };

    await _emailService.SendEmailAsync(notification);
}
```

### 3. Notificar Atribuição de Tarefa

```csharp
// TaskService.cs
public async Task NotifyTaskAssigned(ProjectTask task, string assignedToEmail)
{
    var email = new EmailRequest
    {
        To = assignedToEmail,
        Subject = $"Nova tarefa atribuída: {task.Title}",
        HtmlBody = $@"
            <h3>{task.Title}</h3>
            <p>{task.Description}</p>
            <p><strong>Data de Vencimento:</strong> {task.DueDate:dd/MM/yyyy}</p>
            <p><strong>Prioridade:</strong> {task.Priority}</p>
        "
    };

    await _emailService.SendEmailAsync(email);
}
```

## 🔐 Segurança

### Boas Práticas

1. **Nunca commitar credenciais**
   ```bash
   # Use variáveis de ambiente
   export SMTPSETTINGS__PASSWORD=sua-senha
   ```

2. **Usar HTTPS em produção**
   ```json
   "SmtpSettings": {
     "EnableSsl": true,
     "Port": 465  // ou 587
   }
   ```

3. **Validar emails**
   ```csharp
   if (!IsValidEmail(request.To))
   {
       return BadRequest("Email inválido");
   }
   ```

4. **Limitar taxa de envio**
   ```csharp
   // Implementar rate limiting
   [RateLimit("5 requests per minute")]
   public async Task SendEmail(EmailRequest request) { }
   ```

5. **Logar tentativas de envio**
   ```csharp
   _logger.LogInformation($"Tentativa de envio para {request.To}");
   ```

## 🐛 Troubleshooting

### Erro: "SMTPException: The SMTP server requires a secure connection"

**Solução:** Ativar SSL/TLS

```json
"SmtpSettings": {
  "EnableSsl": true,
  "Port": 587
}
```

### Erro: "Invalid credentials"

**Solução:** Verificar username e password

```bash
# Para Gmail, usar senha de app (não a senha da conta)
# https://myaccount.google.com/apppasswords
```

### Erro: "Timeout"

**Solução:** Aumentar timeout

```json
"SmtpSettings": {
  "Timeout": 60000  // 60 segundos
}
```

### Email vai para Spam

**Solução:** Configurar SPF/DKIM/DMARC

```bash
# No seu domínio, adicionar registros DNS:
# SPF: v=spf1 include:sendgrid.net ~all
# DKIM: Configurar no painel do seu provedor
# DMARC: v=DMARC1; p=none
```

## 📚 Referências

- [SmtpClient Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient)
- [MailMessage Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.mailmessage)
- [Gmail SMTP Setup](https://support.google.com/mail/answer/7126229)
- [SPF/DKIM/DMARC Guide](https://www.cloudflare.com/learning/dns/dnssec/spf-record/)

## 💡 Dicas

1. **Usar templates HTML** para emails mais profissionais
2. **Enviar em lote** para múltiplos destinatários
3. **Testar com SendGrid ou Mailgun** em desenvolvimento
4. **Monitorar taxa de entrega** em produção
5. **Implementar fila de emails** para alto volume
