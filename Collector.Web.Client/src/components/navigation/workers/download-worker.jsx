import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';

export default function DownloadWorker({ worker, progress }) {
    const navigate = useNavigate();

    const eventName = progress?.eventName || '';
    const message = progress?.message || '';
    const status = progress?.status || '';
    const processed = progress?.processed || 0;
    const saved = progress?.saved || 0;
    const links = progress?.links || 0;

    const getStatus = () => {
        if (message) return message;
        if (status) return status;
        return 'Processing downloads...';
    };

    const getIcon = () => {
        if (eventName === 'DownloadError') return 'error';
        if (eventName === 'DownloadComplete') return 'check_circle';
        return 'download';
    };

    const isComplete = eventName === 'DownloadComplete';
    const isError = eventName === 'DownloadError';
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
            navigate('/admin/downloads');
        }
    };

    return (
        <div
            className={`worker-item download-worker ${isError ? 'has-error' : ''}`}
            onClick={handleClick}
            style={{ cursor: 'pointer' }}
        >
            <div className="worker-icon">
                <Icon name={getIcon()} />
            </div>
            <div className="worker-info">
                <div className="worker-title">Download Worker</div>
                <div className="worker-status">{getStatus()}</div>
                {(processed > 0 || saved > 0 || links > 0) && (
                    <div className="worker-stats">
                        {processed > 0 && <span>Processed: {processed}</span>}
                        {saved > 0 && <span>Saved: {saved}</span>}
                        {links > 0 && <span>Links: {links}</span>}
                    </div>
                )}
                {isActive && (
                    <div className="worker-progress-bar">
                        <div className="worker-progress-fill indeterminate" />
                    </div>
                )}
            </div>
        </div>
    );
}
