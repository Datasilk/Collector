import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';

export default function ChatWorker({ worker, progress }) {
    const navigate = useNavigate();

    // Extract progress information
    const eventName = progress?.eventName || '';
    const percent = progress?.percent || 0;
    const message = progress?.message || '';
    const tool = progress?.tool || '';
    const error = progress?.error || '';

    // Determine status display
    const getStatus = () => {
        switch (eventName) {
            case 'Planning':
                return 'Analyzing request...';
            case 'PlanStarted':
                return 'Starting plan execution...';
            case 'PlanProgress':
                return message || 'Executing...';
            case 'ToolError':
                return `Error in ${tool}: ${error}`;
            case 'ToolComplete':
                return `${tool} completed`;
            case 'PlanCompleted':
                return 'Plan execution completed';
            case 'ChatResponse':
                return 'Response ready';
            default:
                return message || 'Processing...';
        }
    };

    // Determine if we should show a progress bar
    const showProgressBar = ['Planning', 'PlanStarted', 'PlanProgress', 'PlanCompleted'].includes(eventName);

    // Determine icon based on state
    const getIcon = () => {
        if (eventName === 'ToolError') return 'error';
        if (eventName === 'PlanCompleted' || eventName === 'ChatResponse') return 'check_circle';
        if (eventName === 'Planning') return 'psychology';
        return 'smart_toy';
    };

    const handleClick = () => {
        if (worker?.url) {
            try {
                const url = new URL(worker.url);
                navigate(url.pathname + url.search + url.hash);
            } catch {
                navigate(worker.url);
            }
        } else if (progress?.chatId) {
            // Navigate to chat if we have a chat ID
            navigate(`/chat/${progress.chatId}`);
        }
    };

    return (
        <div
            className={`worker-item chat-worker ${eventName === 'ToolError' ? 'has-error' : ''}`}
            onClick={handleClick}
            style={{ cursor: (worker?.url || progress?.chatId) ? 'pointer' : 'default' }}
        >
            <div className="worker-icon">
                <Icon name={getIcon()} />
            </div>
            <div className="worker-info">
                <div className="worker-title">AI Assistant</div>
                <div className="worker-status">{getStatus()}</div>
                {showProgressBar && (
                    <div className="worker-progress-bar">
                        <div
                            className="worker-progress-fill"
                            style={{ width: `${percent}%` }}
                        />
                    </div>
                )}
            </div>
            {showProgressBar && <div className="worker-percent">{percent}%</div>}
        </div>
    );
}
