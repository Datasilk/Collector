import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';

export default function VideoWorker({ worker, progress }) {
    const navigate = useNavigate();
    const progressPercent = progress?.progress || 0;
    const status = progress?.status || 'Processing...';
    const title = progress?.title || 'Video Download';
    const handleClick = () => {
        if (worker?.url) {
            // Extract pathname from the full URL
            try {
                const url = new URL(worker.url);
                navigate(url.pathname + url.search + url.hash);
            } catch {
                // If URL parsing fails, try using it directly as a path
                navigate(worker.url);
            }
        }
    };

    return (
        <div className="worker-item video-worker" onClick={handleClick} style={{ cursor: worker?.url ? 'pointer' : 'default' }}>
            <div className="worker-icon">
                <Icon name="videocam" />
            </div>
            <div className="worker-info">
                <div className="worker-title">{title}</div>
                <div className="worker-status">{status}</div>
                <div className="worker-progress-bar">
                    <div 
                        className="worker-progress-fill" 
                        style={{ width: `${progressPercent}%` }}
                    />
                </div>
            </div>
            <div className="worker-percent">{progressPercent}%</div>
        </div>
    );
}
