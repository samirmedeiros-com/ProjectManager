using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ICommentService _commentService;
    private readonly ApplicationDbContext _context;

    public ProjectsController(IProjectService projectService, ICommentService commentService, ApplicationDbContext context)
    {
        _projectService = projectService;
        _commentService = commentService;
        _context = context;
    }

    private string? GetUserRole()
    {
        return User.FindFirst("role")?.Value ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
    }

    private bool IsGestor()
    {
        var role = GetUserRole();
        return role == "Gestor";
    }

    private bool IsGestorOrAdmin()
    {
        var role = GetUserRole();
        return role == "Gestor" || role == "Admin";
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAllProjects()
    {
        var projects = await _projectService.GetAllProjects();
        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int id)
    {
        var project = await _projectService.GetProjectById(id);
        if (project == null)
            return NotFound();

        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (!IsGestorOrAdmin())
            return BadRequest(new { message = "Apenas Gestores e Admins podem criar projetos" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.CreateProject(request);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var project = await _projectService.UpdateProject(id, request, userEmail, GetUserRole());
        if (project == null)
            return Unauthorized(new { message = "Você não tem permissão para editar este projeto" });

        return Ok(project);
    }

    [HttpPut("{id}/manager")]
    public async Task<IActionResult> UpdateProjectManager(int id, [FromBody] UpdateManagerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var project = await _projectService.UpdateProjectManager(id, request.Manager, userEmail, GetUserRole());
        if (project == null)
            return Unauthorized(new { message = "Você não tem permissão para editar este projeto" });

        return Ok(project);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateProjectStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var project = await _projectService.UpdateProjectStatus(id, request.Status, userEmail, GetUserRole());
        if (project == null)
            return Unauthorized(new { message = "Você não tem permissão para editar este projeto" });

        return Ok(project);
    }

    [HttpPut("{id}/owner")]
    public async Task<IActionResult> UpdateProjectOwner(int id, [FromBody] UpdateOwnerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var project = await _projectService.UpdateProjectOwner(id, request.OwnerId, userEmail, GetUserRole());
        if (project == null)
            return Unauthorized(new { message = "Você não tem permissão para editar este projeto" });

        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        if (!IsGestorOrAdmin())
            return BadRequest(new { message = "Apenas Gestores e Admins podem deletar projetos" });

        var success = await _projectService.DeleteProject(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<ProjectMemberDto>>> GetProjectMembers(int id)
    {
        var members = await _projectService.GetProjectMembers(id);
        return Ok(members);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddProjectMember(int id, [FromBody] AddProjectMemberRequest request)
    {
        var success = await _projectService.AddProjectMember(id, request.UserId, request.Role ?? "Membro");
        if (!success)
            return BadRequest(new { message = "Falha ao adicionar membro" });

        return Ok(new { message = "Membro adicionado com sucesso" });
    }

    [HttpDelete("members/{memberId}")]
    public async Task<IActionResult> RemoveProjectMember(int memberId)
    {
        var success = await _projectService.RemoveProjectMember(memberId);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetProjectComments(int id)
    {
        var comments = await _commentService.GetProjectComments(id);
        return Ok(comments);
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> CreateComment(int id, [FromBody] CreateCommentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userEmail = User.FindFirst("email")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        try
        {
            var comment = await _commentService.CreateComment(id, userEmail, request);
            return CreatedAtAction(nameof(GetProjectComments), new { id }, comment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<ProjectHistoryDto>>> GetProjectHistory(int id)
    {
        var history = await _commentService.GetProjectHistory(id);
        return Ok(history);
    }
}
