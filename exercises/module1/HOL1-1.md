# Lab 1.1 - Prompting and Performance Tuning

In this lab, you will explore GitHub Models and learn the fundamentals of prompt engineering. You will experiment with system and user prompts, model parameters such as temperature and top-p, and evaluate how different configurations influence the model’s output.

This Lab is designed to let you experiment with prompts, model settings and different models. Do not spend too much time in each step. We will use more of the models in the next labs.

---

## Enable GitHub Models

**Goal:** Enable GitHub Models in your repository and open the Playground to run your first prompts.

>Start with new prompts by clicking on the 🗑️ icon to clear previous content in the Playground. The chat has "memory" that retains context from previous interactions. To have a clear view, removing previous content is recommended.

### Steps

1. Go to your **GitHub repository**.

2. Click **Settings**.

3. In the left menu, locate and enable **Models**.

4. Confirm that the **Models** menu appears at the top.

   * If it does not appear, perform a hard refresh (`Ctrl+Shift+R` or `Cmd+Shift+R`).

   ![](./images/Models-Menu.png)

5. Click **Models** in the top menu.

6. Open **Playground**.

7. Select the **OpenAI GPT-4.1** model from the list.

---

## Prompting Basics

**Goal:** Understand how prompt wording, role, tone, and structure affects model responses.

### Steps


1. In **GitHub Models → Playground**, select **OpenAI GPT-4.1**.

2. Paste the following user prompt:

   ```txt
   Write an email to a GloboTicket customer explaining a refund is approved for order #GT-48321.
   ```

3. You can influence behavior of the chatbot bu adding a system prompt. Add this system prompt and run again:

   ```txt
   You are a GloboTicket support agent. Tone: warm but concise. ≤120 words. Include refund amount and resolution time (3–5 business days).
   ```

4. Change only the tone and re-run:

   * “Use a formal tone of voice.”
   * “Use a ‘pop rock fan’ tone of voice.”

5. Add structure to the system prompt and run again:

   ```txt
   Use greeting, 3 bullet points, closing.
   ```
6. Remove the System prompt

> Reflect: How did the output change with each adjustment? Which part—role, tone, or structure—had the largest impact?

---

## Temperature and Creativity

**Goal:** Observe how the temperature, top_p and frequency parameter affects creativity and randomness in model responses.

### Steps

1. Use this prompt:

   ```txt
   I am visiting a concert at Madison Square Garden. What can I do before the concert starts ?
   ```

2. Play around with temperature, top_p, frequency and presence penalty to see if you difference in reponse

**Temperature** controls how random or creative a language model’s responses are — lower values make answers more focused and deterministic, while higher values make them more varied and imaginative.

**Top-p (nucleus sampling)** limits the model’s word choices to the smallest set whose combined probability is at least *p*, so lower values make responses more focused and higher values allow more diverse phrasing.

**Frequency penalty** reduces the likelihood of the model repeating the same words or phrases within a response. Higher values make the output less repetitive and encourage varied wording.

**Presence penalty** discourages the model from reusing words or ideas it has already mentioned, promoting the introduction of new topics or concepts in the response.

>Reflect: Do you notice differences in creativity or relevance with different settings? Which combinations worked best for your prompt?

Do not spend too much time here—just get a feel for how these parameters influence output. In the newer models you can not influence the output that much with these parameters. With Semantic Kernel we can still send these parameters so it is good to know that they exist. But in most cases you will get good results with the default settings.

---

## Multi-Step Prompt Engineering

**Goal:** Create a sequence that transforms free text into structured data, as in real-world LLM-powered applications.

### Steps

1. Use one venue policy (Markdown) as input.

2. Generate a short summary:

   ```txt
   Summarize this policy for support agents (≤120 words).
   ```

3. Extract structured JSON:

   ```txt
   Extract into JSON with keys: {bag_max_cm:[L,W,H], backpacks_rule, bottle_empty_allowed, reentry, cashless, service_animals_only, accessibility:{wheelchair,lifts,hearing_loops}}. Output ONLY JSON.
   ```

> Reflect: How could you use an LLM to format or parse text when working with APIs?

---

## Model Comparison

**Goal:** Evaluate differences in tone, speed, reasoning, and creativity across models.

### Steps

1. Select two models in GitHub Models, such as **OpenAI GPT-4.1** and **Grok 3 Mini**.

2. Use this prompt for both:

   ```txt
   Draft a 100-word apology email for a payment outage impacting 2% of GloboTicket checkouts in the EU on 2025-09-28. Include next steps and refund guidance.
   ```

3. Have each model self-evaluate:

   ```txt
   Create a quick scorecard: Tone, Clarity, Actionability, Factual control, Hallucinations.
   ```

4. (Optional) Ask for a **PowerShell script** to send the email.

> Reflect: Which model generated the most usable output? Which evaluation criteria mattered most for your scenario?

---

This concludes Lab 1.1.
