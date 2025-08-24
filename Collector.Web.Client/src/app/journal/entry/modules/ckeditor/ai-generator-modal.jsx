import React, { useState } from 'react';
import TextArea from '@/components/forms/textarea';
import Select from '@/components/forms/select';
import Modal from '@/components/ui/modal';
import * as signalR from '@microsoft/signalr';

/**
 * AI Generator Modal Component
 * Provides a form for entering AI content generation prompts and handles SignalR communication
 */
export default function AIGeneratorModal({ module, onClose, onGenerated }) {
    const [prompt, setPrompt] = useState('');
    const [isGenerating, setIsGenerating] = useState(false);
    const [error, setError] = useState('');
    
    // Get previous user inputs from module if available
    const previousInputs = module?.userInput || [];

    const handleSubmit = (e) => {
        e.preventDefault();
        if (prompt.trim()) {
            generateContent(prompt);
        }
    };
    
    // Handle selecting a previous prompt from the dropdown
    const handleSelectInput = (e) => {
        setPrompt(e.target.value);
    };

    const generateContent = (prompt) => {
        setIsGenerating(true);
        setError('');

        // Connect to SignalR hub
        const conn = new signalR.HubConnectionBuilder()
            .withUrl(import.meta.env.VITE_API_URL + '/text-editor', {
                withCredentials: true,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 1000, 5000, 10000]) // Retry connection if it fails
            .configureLogging(signalR.LogLevel.Information)
            .build();

        conn.start().then(() => {
            // Listen for content generation response
            conn.on('ReceiveContent', message => {
                try {
                    const generatedContent = message;
                    // Call the callback with the generated content
                    onGenerated(prompt, generatedContent);

                    // Close the modal
                    onClose();
                } catch (err) {
                    console.error('Error processing AI response:', err);
                    setError('Error generating content. Please try again.');
                }

                // Stop the connection
                conn.stop();
                setIsGenerating(false);
            });

            // Send the prompt to generate content
            conn.invoke('GenerateContent', prompt)
                .catch(err => {
                    console.error('Error invoking GenerateContent:', err);
                    setError('Error sending request to AI service. Please try again.');

                    // Stop the connection
                    conn.stop();
                    setIsGenerating(false);
                });
        }).catch(err => {
            console.error('Error starting SignalR connection:', err);
            setError('Error connecting to AI service. Please try again.');
            setIsGenerating(false);
        });
    };

    return (
        <Modal title="AI Content Generator" onClose={onClose}>
            {isGenerating && (
                <div className="ai-generator-loading">
                    <p>Generating content...</p>
                </div>
            )}
            {error && (
                <div className="ai-generator-error">
                    <p>{error}</p>
                    <div className="buttons">
                        <button className="btn primary" onClick={onClose}>Close</button>
                    </div>
                </div>
            )}
            {!isGenerating && !error && (
                <form onSubmit={handleSubmit}>
                    <div className="prompt-inputs">
                        <TextArea
                            label="Describe the content you want to generate"
                            name="ai-prompt"
                            placeholder="Example: Write a paragraph about climate change and its effects on agriculture"
                            rows={5}
                            autoResize={true}
                            value={prompt}
                            onInput={(e) => setPrompt(e.target.value)}
                        />
                        
                        {previousInputs.length > 0 && (
                            <Select
                                label="Previous prompts"
                                name="previous-prompts"
                                options={[
                                    { label: "Select a previous prompt", value: "" },
                                    ...previousInputs.map(input => ({
                                        label: input.length > 60 ? input.substring(0, 60) + '...' : input,
                                        value: input
                                    }))
                                ]}
                                value=""
                                onChange={handleSelectInput}
                            />
                        )}
                    </div>

                    <div className="buttons">
                        <button type="button" className="cancel" onClick={onClose}>Cancel</button>
                        <button type="submit" disabled={!prompt.trim()}>Generate</button>
                    </div>
                </form>
            )}
        </Modal>
    );
}
