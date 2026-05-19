using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartNotes.Models;

namespace SmartNotes.Services;

public class GroqNotesService
{
    private readonly HttpClient _http;

    // Small fast model for decomposition (cheap, instant)
    private const string FastModel = "llama-3.1-8b-instant";

    // Large model for deep textbook-quality notes
    private const string DeepModel = "llama-3.3-70b-versatile";

    private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    public GroqNotesService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _http = httpFactory.CreateClient("groq");
        var apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<NoteGenerationResult> GenerateNotesAsync(NoteGenerationRequest request)
    {
        var result = new NoteGenerationResult
        {
            Topic    = request.Topic,
            Subject  = request.Subject,
            ExamType = request.ExamType
        };

        try
        {
            // Stage 1: Fast model — decompose topic into subtopics
            var subtopics = await DecomposeTopicAsync(request.Topic, request.Subject);
            result.Subtopics = subtopics;

            // Stage 2: Deep model — sequential with small delays to respect free tier TPM limit
            result.NotesContent     = await GenerateDetailedNotesAsync(request, subtopics);
            await Task.Delay(4000);
            result.SummaryContent   = await GenerateSummaryAsync(request, subtopics);
            await Task.Delay(4000);
            result.QuestionsContent = await GenerateQuestionsAsync(request, subtopics);
            await Task.Delay(4000);
            result.DiagramsContent  = await GenerateDiagramsAsync(request, subtopics);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error   = ex.Message;
        }

        return result;
    }

    private async Task<string> CallGroqAsync(string model, string systemPrompt, string userPrompt, int maxTokens)
    {
        var payload = new
        {
            model,
            max_tokens = maxTokens,
            temperature = 0.3,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(GroqEndpoint, content);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Groq API error {response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? string.Empty;
    }

    // ---- Stage 1: Decompose (fast model) ----
    private async Task<List<string>> DecomposeTopicAsync(string topic, string subject)
    {
        var system = "You are an expert engineering professor. Return ONLY valid JSON arrays, no explanation, no markdown.";
        var user   = $@"Break down the engineering topic ""{topic}"" from the subject ""{subject}"" into 5-7 focused subtopics that together cover it comprehensively for a BTech/BE student.

Return ONLY a JSON array of strings. Example:
[""Subtopic A"", ""Subtopic B"", ""Subtopic C""]";

        var raw = await CallGroqAsync(FastModel, system, user, 400);
        raw = raw.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string> { topic };
        }
        catch
        {
            return new List<string> { topic };
        }
    }

    // ---- Stage 2a: Detailed Notes (deep model) ----
    private async Task<string> GenerateDetailedNotesAsync(NoteGenerationRequest request, List<string> subtopics)
    {
        var subtopicList = string.Join("\n", subtopics.Select((s, i) => $"{i + 1}. {s}"));

        var system = @"You are a senior engineering professor writing university-level textbook notes.
Your notes must be technically precise, deeply explained, and contain real engineering examples with actual values.
Never oversimplify. Write like a textbook, not like a chatbot summary.
Output only valid HTML using the allowed tags.";

        var user = $@"Write comprehensive textbook-quality study notes on:

Topic: {request.Topic}
Subject: {request.Subject}

Subtopics to cover (cover ALL of them in depth):
{subtopicList}

STRICT REQUIREMENTS — these notes must beat human-written notes:
- Every concept must have its underlying theory explained (not just what, but WHY)
- Every subtopic needs at least ONE worked engineering example with real numerical values
- Correct common misconceptions explicitly
- Include governing equations/formulas written clearly in text form
- Note edge cases and conditions where assumptions break down
- Connect to real-world engineering applications and industry use

FORMAT USING ONLY THESE HTML TAGS:
<h2> — main topic title
<h3> — each subtopic heading
<h4> — sub-section headings
<p>  — paragraphs
<ul><li> — bullet lists
<ol><li> — numbered steps/derivations
<div class=""highlight-box""> — important formulas, key theorems, warnings
<div class=""example-box""> — worked examples with values
<strong> — key terms on first use
<em> — emphasis

Write at BTech/BE university textbook level. Be detailed, accurate, and thorough.";

        return await CallGroqAsync(DeepModel, system, user, 2500);
    }

    // ---- Stage 2b: Summary (deep model) ----
    private async Task<string> GenerateSummaryAsync(NoteGenerationRequest request, List<string> subtopics)
    {
        var subtopicList = string.Join(", ", subtopics);

        var system = @"You are an expert engineering professor creating a high-density revision summary.
Every word must carry information. No filler. Output only valid HTML.";

        var user = $@"Create a concise but complete revision summary for:

Topic: {request.Topic}
Subject: {request.Subject}
Subtopics covered: {subtopicList}

FORMAT AS HTML:
<h2>Quick Revision: {request.Topic}</h2>
<p>3-4 sentence paragraph capturing the core essence of the entire topic</p>

<div class=""summary-grid"">
  <div class=""summary-box""><h4>Core Principle</h4><p>The fundamental engineering concept in 2 precise lines</p></div>
  <div class=""summary-box""><h4>Key Subtopics</h4><p>Bullet list of all subtopics with one-line descriptions</p></div>
  <div class=""summary-box""><h4>Critical Formulas</h4><p>Most important equations/rules for this topic</p></div>
  <div class=""summary-box""><h4>Engineering Application</h4><p>Where and how this is used in real engineering practice</p></div>
  <div class=""summary-box""><h4>Exam Focus</h4><p>What BTech/BE exams most commonly ask about this topic</p></div>
</div>

<div class=""highlight-box"">Golden Rule: one sentence that captures the entire topic's essence</div>

<div class=""tag-row""><span class=""tag"">keyword1</span> ... 6-8 key terms as tags</div>";

        return await CallGroqAsync(DeepModel, system, user, 1000);
    }

    // ---- Stage 2c: Questions — Solved Examples + Assignment (deep model) ----
    private async Task<string> GenerateQuestionsAsync(NoteGenerationRequest request, List<string> subtopics)
    {
        var subtopicList = string.Join(", ", subtopics);

        var system = @"You are an expert engineering professor creating study material with solved examples and assignment questions.
Output only valid HTML exactly as instructed. No markdown, no code fences.";

        var user = $@"Create a complete question set for:

Topic: {request.Topic}
Subject: {request.Subject}
Subtopics: {subtopicList}

OUTPUT TWO SECTIONS IN HTML:

════════════════════════════
SECTION 1 — SOLVED EXAMPLES
════════════════════════════
Generate exactly 3 solved examples at different difficulty levels.
Each solved example must show the COMPLETE step-by-step solution immediately (not hidden).

Use this HTML structure for each:
<div class=""solved-question"">
  <div class=""solved-header"">
    <span class=""difficulty-badge easy"">EASY</span>
    <p class=""q-text""><strong>Example 1:</strong> Question text here</p>
  </div>
  <div class=""solved-solution"">
    <h4>✅ Solution</h4>
    <p>Step 1: ...</p>
    <p>Step 2: ...</p>
    <div class=""highlight-box"">Final Answer: ...</div>
  </div>
</div>

Use difficulty-badge classes: ""easy"" / ""medium"" / ""hard"" for the 3 examples respectively.
Make examples progressively harder. Use real engineering values in numerical problems.

════════════════════════════
SECTION 2 — ASSIGNMENT QUESTIONS
════════════════════════════
Generate exactly 5 assignment questions at varying difficulty.
The answer must be HIDDEN inside a div with class ""answer-content"" and style=""display:none"".

Use this EXACT HTML structure for each:
<div class=""assign-question"">
  <div class=""assign-header"">
    <span class=""difficulty-badge easy"">EASY</span>
    <p class=""q-text""><strong>Q1.</strong> Question text here <span class=""marks"">[X Marks]</span></p>
  </div>
  <button class=""reveal-btn"" onclick=""revealAnswer(this)"">👁 Reveal Answer</button>
  <div class=""answer-content"" style=""display:none"">
    <h4>Answer Key:</h4>
    <p>Complete answer here with explanation</p>
  </div>
</div>

Use difficulty levels: Easy, Easy, Medium, Medium, Hard across the 5 questions.
All questions must be technically specific to {request.Topic}.

Wrap the entire output in:
<h2>🧠 Questions: {request.Topic}</h2>
<h3>📖 Solved Examples</h3>
[3 solved examples]
<h3>📝 Assignment Questions</h3>
[5 assignment questions]";

        return await CallGroqAsync(DeepModel, system, user, 2000);
    }

    // ---- Stage 2d: Diagrams using Mermaid.js (deep model) ----
    private async Task<string> GenerateDiagramsAsync(NoteGenerationRequest request, List<string> subtopics)
    {
        var system = @"You are an expert engineering professor creating technical diagrams using Mermaid.js syntax.
You must output only valid HTML. Mermaid diagram code goes inside <div class=""mermaid""> tags.
Never use markdown code fences. Only output raw HTML.";

        var user = $@"Create 2-3 technical Mermaid.js diagrams for:

Topic: {request.Topic}
Subject: {request.Subject}

For each diagram:
1. Choose the most appropriate Mermaid diagram type:
   - ""flowchart TD"" for processes, algorithms, flows
   - ""graph LR"" for relationships, connections
   - ""sequenceDiagram"" for sequences, protocols
   - ""classDiagram"" for class/object structures
   - ""stateDiagram-v2"" for state machines
   - ""erDiagram"" for entity relationships
   - ""block-beta"" for block/system diagrams

2. Use proper engineering labels and notation
3. Keep node text short and clear
4. Use subgraphs to group related components where needed

Use this EXACT HTML structure for each diagram:

<div class=""diagram-section"">
  <h3>📐 Figure N: [Descriptive Engineering Title]</h3>
  <div class=""mermaid"">
[valid mermaid syntax here — no code fences, just raw mermaid code]
  </div>
  <div class=""diagram-explanation"">
    <h4>Explanation</h4>
    <p>Detailed technical explanation of what the diagram shows, how to read it, and what each component represents in the context of {request.Topic}.</p>
    <ul>
      <li>Label 1: what it represents</li>
      <li>Label 2: what it represents</li>
    </ul>
  </div>
</div>

Generate diagrams that are genuinely useful for understanding {request.Topic} in {request.Subject}.
Make the Mermaid syntax valid and renderable — test each node connection mentally before writing.";

        return await CallGroqAsync(DeepModel, system, user, 1500);
    }
}
