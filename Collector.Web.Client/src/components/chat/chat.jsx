import React, { useState, useEffect } from 'react';
import Icon from '@/components/ui/icon';
import ChatWindow from './chat-window';
import { useSession } from '@/context/session';
import { LLMs } from '@/api/user/llms';
import './chat.css';

export default function Chat() {
    const session = useSession();
    const [chatWindows, setChatWindows] = useState([]);
    const [availableLLMs, setAvailableLLMs] = useState([]);
    const [nextWindowId, setNextWindowId] = useState(0);

    useEffect(() => {
        const fetchLLMs = async () => {
            try {
                const { getAvailable } = LLMs(session);
                const response = await getAvailable();
                if (response.data && response.data.success) {
                    const llms = response.data.data.map(llm => ({
                        value: llm.model,
                        label: llm.model
                    }));
                    setAvailableLLMs(llms);
                }
            } catch (error) {
                console.error('Error fetching LLMs:', error);
            }
        };

        if (session?.user) {
            fetchLLMs();
        }
    }, [session]);

    const handleNewChat = () => {
        // Add a new chat window with stable ID and null chatId
        const windowId = nextWindowId;
        setNextWindowId(nextWindowId + 1);
        setChatWindows([...chatWindows, { windowId, chatId: null }]);
    };

    const handleCloseChat = (windowId) => {
        setChatWindows(chatWindows.filter(w => w.windowId !== windowId));
    };

    const handleUpdateChatId = (windowId, newChatId) => {
        setChatWindows(chatWindows.map(w => 
            w.windowId === windowId ? { ...w, chatId: newChatId } : w
        ));
    };

    return (
        <>
            <div className="chat-windows">
                {chatWindows.map((window) => (
                    <ChatWindow
                        key={`chat-window-${window.windowId}`}
                        windowId={window.windowId}
                        chatId={window.chatId}
                        availableLLMs={availableLLMs}
                        openChatIds={chatWindows.filter(w => w.chatId !== null).map(w => w.chatId)}
                        onClose={handleCloseChat}
                        onUpdateId={handleUpdateChatId}
                    />
                ))}
            </div>
            <button className="chat-fab" onClick={handleNewChat} title="New Chat">
                <Icon name="add" />
            </button>
        </>
    );
}
