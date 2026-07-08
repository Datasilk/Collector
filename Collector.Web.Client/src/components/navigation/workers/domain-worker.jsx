import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import { useWorkerHub } from '@/context/workerhub';

export default function DomainWorker({ worker, progress }) {
    const navigate = useNavigate();
    const { stop } = useWorkerHub();

    const eventName = progress?.eventName || '';
    const message = progress?.message || '';
    const status = progress?.status || '';
    const processed = progress?.processed || 0;
    const domain = progress?.domain || '';

    const getStatus = () => {
        if (message) return message;
        if (status) return status;
        if (domain) return `Analyzing ${domain}...`;
        return 'Domain analyzer running...';
    };

    const getIcon = () => {
        if (eventName === 'DomainAnalysisError') return 'error';
        if (eventName === 'DomainAnalysisComplete') return 'check_circle';
        return 'batch_prediction';
    };

    const isComplete = eventName === 'DomainAnalysisComplete';
    const isError = eventName === 'DomainAnalysisError';
    const isActive = !isComplete && !isError;

    const handleClick = () => {
        if (worker?.url) {
            try {
                const url = new URL(worker.url);
                navigate(url.pathname + url.search + url.hash);
            } catch {
                navigate(worker.url);
            }
        } else {
            navigate('/admin/domains');
        }
    };

    const handleStop = async (e) => {
        e.stopPropagation();
        if (worker?.workerId) {
            try {
                await stop(worker.workerId);
            } catch (err) {
                console.error('Failed to stop domain analyzer:', err);
            }
        }
    };

    return (
        <div
            className={`worker-item domain-worker ${isError ? 'has-error' : ''}`}
            onClick={handleClick}
            style={{ cursor: 'pointer' }}
        >
            <div className="worker-icon">
                <Icon name={getIcon()} />
            </div>
            <div className="worker-info">
                <div className="worker-title">Domain Analyzer</div>
                <div className="worker-status">{getStatus()}</div>
                {processed > 0 && (
                    <div className="worker-stats">
                        <span>Analyzed: {processed}</span>
                    </div>
                )}
                {isActive && (
                    <div className="worker-progress-bar">
                        <div className="worker-progress-fill indeterminate" />
                    </div>
                )}
            </div>
            {isActive && (
                <button className="icon" title="Stop" onClick={handleStop}>
                    <Icon name="stop" />
                </button>
            )}
        </div>
    );
}
