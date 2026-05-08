import React, { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
import { Chats } from '@/api/user/chats';

export default function ChatWindow({ windowId, chatId, availableLLMs, openChatIds, onClose, onUpdateId }) {
    const session = useSession();
    const workerHub = useWorkerHub();
    
    const [message, setMessage] = useState('');
    const [messages, setMessages] = useState([]);
    const [llm, setLlm] = useState(availableLLMs.length > 0 ? availableLLMs[0].value : 'gpt-4');
    const [mode, setMode] = useState('ask');
    const [title, setTitle] = useState('New Chat');
    const [isMinimized, setIsMinimized] = useState(false);
    const [isMaximized, setIsMaximized] = useState(false);
    const [showLlmDropdown, setShowLlmDropdown] = useState(false);
    const [showModeDropdown, setShowModeDropdown] = useState(false);
    const [showChatDropdown, setShowChatDropdown] = useState(false);
    const [isCreating, setIsCreating] = useState(chatId === null);
    const [workerId, setWorkerId] = useState(null);
    const [availableChats, setAvailableChats] = useState([]);
    const [selectedExistingChat, setSelectedExistingChat] = useState(null);
    const [isThinking, setIsThinking] = useState(false);
    
    const messagesEndRef = useRef(null);
    const llmDropdownRef = useRef(null);
    const modeDropdownRef = useRef(null);
    const chatDropdownRef = useRef(null);

    const llmOptions = availableLLMs.length > 0 ? availableLLMs : [
        { value: 'gpt-4', label: 'GPT-4' }
    ];

    const modeOptions = [
        { value: 'ask', label: 'Ask' },
        { value: 'agent', label: 'Agent' }
    ];

    // Fetch available chats when window opens with no chatId
    useEffect(() => {
        if (chatId === null && session?.user) {
            const fetchChats = async () => {
                try {
                    const { list } = Chats(session);
                    const response = await list(0, 20);
                    if (response.data && response.data.success) {
                        // Filter out chats that are already open
                        const filtered = response.data.data.filter(chat => chat.id == null || !openChatIds.includes(chat.id));
                        setAvailableChats(filtered);
                    }
                } catch (error) {
                    console.error('Error fetching chats:', error);
                }
            };
            fetchChats();
        }
    }, [chatId, session, openChatIds]);

    // Listen for worker messages
    useEffect(() => {
        if (!workerId || !workerHub) return;

        const handleWorkerMessage = ({ eventName, payload }) => {
            switch (eventName) {
                case 'ChatCreated':
                    setIsCreating(false);
                    setTitle(payload.title);
                    onUpdateId(windowId, payload.id);
                    break;
                case 'AssistantPlan':
                    // Show the plan message in chat (before progress starts)
                    setMessages(prev => [...prev, { role: 2, content: payload.message, isPlan: true }]);
                    break;
                case 'Planning':
                    // Remove all existing progress messages and add new one at the end
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { role: 2, content: payload.message, isProgress: true }];
                    });
                    break;
                case 'OllamaRequest':
                    // Log Ollama request (server only sends in development)
                    console.log(`[Ollama Request - ${payload.type}]`, payload.prompt);
                    break;
                case 'OllamaResponse':
                    // Log Ollama raw response (server only sends in development)
                    console.log(`[Ollama Response - ${payload.type}]`, payload.rawResponse);
                    break;
                case 'PlanStarted':
                    // Remove all existing progress messages and add new one at the end
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { role: 2, content: `Executing ${payload.stepCount} step(s)...`, isProgress: true }];
                    });
                    break;
                case 'PlanProgress':
                    // Remove all existing progress messages and add new one at the end
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { role: 2, content: `${payload.message} (${payload.percent}%)`, isProgress: true }];
                    });
                    break;
                case 'ToolError':
                    // Log full error details to browser console
                    if (payload.exception) {
                        console.error(`Tool Error in ${payload.tool}:`, {
                            error: payload.error,
                            exception: payload.exception,
                            stackTrace: payload.exception.stackTrace
                        });
                    } else {
                        console.error(`Tool Error in ${payload.tool}:`, payload.error);
                    }
                    setMessages(prev => [...prev, { role: 2, content: `Error in ${payload.tool}: ${payload.error}`, isError: true }]);
                    break;
                case 'ToolComplete':
                    // Remove all existing progress messages and add new one at the end
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { role: 2, content: `${payload.tool}: ${payload.message}`, isProgress: true }];
                    });
                    break;
                case 'PlanCompleted':
                    // Remove all progress messages (isProgress = true)
                    setMessages(prev => prev.filter(msg => !msg.isProgress));
                    break;
                case 'ChatResponse':
                    // Remove all progress messages before showing final response
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { id: payload.id, role: payload.role, content: payload.content }];
                    });
                    setIsThinking(false);
                    break;
                case 'ChatMessage':
                    // Message from tool with optional link (e.g., journal entry created)
                    setMessages(prev => [...prev, {
                        id: `msg-${Date.now()}`,
                        role: 2,
                        content: payload.message,
                        linkUrl: payload.linkUrl,
                        linkText: payload.linkText
                    }]);
                    break;
                case 'ChatHistoryLoaded':
                    setMessages(payload.messages.map(m => ({
                        id: m.id,
                        role: m.role,
                        content: m.content
                    })));
                    break;
                case 'WorkerProgress':
                    // Handle worker progress updates (debug, status, etc.)
                    if (payload.isDevelopment) {
                        console.log(`[Worker ${payload.type || 'Progress'}]`, payload.message || payload);
                    }
                    break;
                case 'ChatError':
                    // Log full error details to browser console
                    if (payload.stackTrace) {
                        console.error('Chat error:', {
                            message: payload.message,
                            stackTrace: payload.stackTrace,
                            type: payload.type,
                            innerException: payload.innerException
                        });
                    } else {
                        console.error('Chat error:', payload.message);
                    }
                    // Clear all progress messages on error
                    setMessages(prev => {
                        const filtered = prev.filter(msg => !msg.isProgress);
                        return [...filtered, { role: 2, content: `Error: ${payload.message}`, isError: true }];
                    });
                    setIsThinking(false);
                    break;
            }
        };

        let unsubscribe = null;
        let isMounted = true;

        workerHub.subscribe(workerId, handleWorkerMessage).then(unsub => {
            if (isMounted) {
                unsubscribe = unsub;
            } else {
                // Component unmounted before subscribe completed, cleanup immediately
                unsub();
            }
        });

        return () => {
            isMounted = false;
            if (unsubscribe) {
                unsubscribe();
            }
        };
    }, [workerId, workerHub, onUpdateId]);

    useEffect(() => {
        if (messagesEndRef.current) {
            messagesEndRef.current.scrollIntoView({ behavior: 'smooth' });
        }
    }, [messages]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (llmDropdownRef.current && !llmDropdownRef.current.contains(event.target)) {
                setShowLlmDropdown(false);
            }
            if (modeDropdownRef.current && !modeDropdownRef.current.contains(event.target)) {
                setShowModeDropdown(false);
            }
            if (chatDropdownRef.current && !chatDropdownRef.current.contains(event.target)) {
                setShowChatDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleSelectExistingChat = async (chat) => {
        setSelectedExistingChat(chat);
        setTitle(chat.title);
        setShowChatDropdown(false);
        setIsCreating(false);
        onUpdateId(windowId, chat.id);

        // Load chat history via API
        try {
            const { getHistory } = Chats(session);
            const response = await getHistory(chat.id);
            if (response.data && response.data.success) {
                const chatData = response.data.data;
                setMessages(chatData.messages.map(m => ({
                    id: m.id,
                    role: m.role,
                    content: m.content
                })));
                setTitle(chatData.title);
            }
        } catch (error) {
            console.error('Error loading chat history:', error);
        }
    };

    const handleCreateNewChat = async () => {
        setShowChatDropdown(false);
        try {
            const newWorkerId = await workerHub.call('chat-worker', 'CreateChat', { title: 'New Chat' });
            setWorkerId(newWorkerId);
        } catch (error) {
            console.error('Error creating chat:', error);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!message.trim()) return;

        try {
            const messageToSend = message;
            setMessage('');

            // Add user message to UI immediately
            setMessages(prev => [...prev, { role: 0, content: messageToSend }]);
            setIsThinking(true);

            // If no workerId exists, create one for new chat
            let currentWorkerId = workerId;
            if (!currentWorkerId) {
                currentWorkerId = await workerHub.call('chat-worker', 'SendMessage', {
                    chatId: chatId,
                    message: messageToSend,
                    model: llm,
                    mode: mode
                });
                setWorkerId(currentWorkerId);
            } else {
                // Send message via existing worker
                await workerHub.call('chat-worker', 'SendMessage', {
                    chatId: chatId,
                    message: messageToSend,
                    model: llm,
                    mode: mode
                }, null, currentWorkerId);
            }
        } catch (error) {
            console.error('Error sending message:', error);
            setIsThinking(false);
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSubmit(e);
        }
    };

    const getLlmLabel = () => llmOptions.find(opt => opt.value === llm)?.label || 'Select LLM';
    const getModeLabel = () => modeOptions.find(opt => opt.value === mode)?.label || 'Select Mode';

    if (isMinimized) {
        return (
            <div className="chat-window minimized">
                <div className="chat-header">
                    <span className="chat-title">{title}</span>
                    <div className="chat-actions">
                        <button className="icon" onClick={() => setIsMinimized(false)} title="Restore">
                            <Icon name="expand_less" />
                        </button>
                        <button className="icon" onClick={() => onClose(windowId)} title="Close">
                            <Icon name="close" />
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={`chat-window ${isMaximized ? 'maximized' : ''}`}>
            <div className="chat-header">
                <span className="chat-title">{title}</span>
                <div className="chat-actions">
                    <button className="icon" onClick={() => { setIsMinimized(true); setIsMaximized(false); }} title="Minimize">
                        <Icon name="minimize" />
                    </button>
                    <button className="icon" onClick={() => setIsMaximized(!isMaximized)} title={isMaximized ? "Restore" : "Maximize"}>
                        <Icon name={isMaximized ? "close_fullscreen" : "open_in_full"} />
                    </button>
                    <button className="icon" onClick={() => onClose(windowId)} title="Close">
                        <Icon name="close" />
                    </button>
                </div>
            </div>
            <div className="chat-messages">
                {messages.length === 0 ? (
                    <div className="chat-empty">
                        <Icon name="chat" />
                        <p>Start a conversation with AI</p>
                        {availableChats.length > 0 && (
                            <div className="chat-selection">
                                <p>Or continue an existing conversation:</p>
                                <div className="custom-dropdown" ref={chatDropdownRef}>
                                    <button
                                        type="button"
                                        className="dropdown-trigger"
                                        onClick={() => setShowChatDropdown(!showChatDropdown)}
                                    >
                                        Select Chat
                                        <Icon name="expand_more" />
                                    </button>
                                    {showChatDropdown && (
                                        <div className="dropdown-menu">
                                            {availableChats.map(chat => (
                                                <div
                                                    key={chat.id}
                                                    className="dropdown-item"
                                                    onClick={() => handleSelectExistingChat(chat)}
                                                >
                                                    {chat.title}
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>
                ) : (
                    <>
                        {messages.map((msg, index) => (
                            <div key={index} className={`chat-message ${msg.role === 0 ? 'user' : msg.role === 1 ? 'assistant' : 'system'} ${msg.isError ? 'error' : ''}`}>
                                <div className="message-content">
                                    {msg.content}
                                    {msg.linkUrl && (
                                        <span className="message-link"> (
                                            <Link to={msg.linkUrl} className="chat-link">{msg.linkText || 'View'}</Link>
                                        )</span>
                                    )}
                                </div>
                            </div>
                        ))}
                        {isThinking && (
                            <div className="chat-message assistant thinking">
                                <div className="message-content">
                                    <div className="thinking-spinner">
                                        <Icon name="autorenew" />
                                    </div>
                                </div>
                            </div>
                        )}
                    </>
                )}
                <div ref={messagesEndRef} />
            </div>
            <div className="chat-input-container">
                <form onSubmit={handleSubmit} className="chat-form">
                    <textarea
                        className="chat-textarea"
                        value={message}
                        onChange={(e) => setMessage(e.target.value)}
                        onKeyDown={handleKeyDown}
                        placeholder="Type your message..."
                        rows="1"
                    />
                    <div className="chat-controls">
                        <div className="chat-dropdowns">
                            <div className="custom-dropdown" ref={modeDropdownRef}>
                                <button
                                    type="button"
                                    className="dropdown-trigger"
                                    onClick={() => setShowModeDropdown(!showModeDropdown)}
                                >
                                    {getModeLabel()}
                                    <Icon name="expand_more" />
                                </button>
                                {showModeDropdown && (
                                    <div className="dropdown-menu">
                                        {modeOptions.map(option => (
                                            <div
                                                key={option.value}
                                                className={`dropdown-item ${mode === option.value ? 'selected' : ''}`}
                                                onClick={() => {
                                                    setMode(option.value);
                                                    setShowModeDropdown(false);
                                                }}
                                            >
                                                {option.label}
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                            <div className="custom-dropdown" ref={llmDropdownRef}>
                                <button
                                    type="button"
                                    className="dropdown-trigger"
                                    onClick={() => setShowLlmDropdown(!showLlmDropdown)}
                                >
                                    {getLlmLabel()}
                                    <Icon name="expand_more" />
                                </button>
                                {showLlmDropdown && (
                                    <div className="dropdown-menu">
                                        {llmOptions.map(option => (
                                            <div
                                                key={option.value}
                                                className={`dropdown-item ${llm === option.value ? 'selected' : ''}`}
                                                onClick={() => {
                                                    setLlm(option.value);
                                                    setShowLlmDropdown(false);
                                                }}
                                            >
                                                {option.label}
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </div>
                        <button type="submit" className="chat-submit" title="Send message">
                            <Icon name="arrow_upward" />
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
