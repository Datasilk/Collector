import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';

export default function YoutubeChannelWorker({ progress }) {
    const navigate = useNavigate();
    const progressPercent = progress?.progress || 0;
    const status = progress?.status || 'Processing...';
    const title = progress?.title || 'YouTube Channel Import';
    const entryUrl = progress?.entryUrl;

    const detail = progress?.total > 0
        ? `${progress.created || 0}/${progress.total} entries · ${progress.downloaded || 0}/${progress.created || progress.total} downloaded` +
          (progress.inFlight > 0 ? ` (${progress.inFlight} in progress)` : '')
        : null;

    const handleClick = () => {
        if (entryUrl) {
            navigate(entryUrl);
        }
    };

    return (
        <div className="worker-item youtube-channel-worker" onClick={handleClick} style={{ cursor: entryUrl ? 'pointer' : 'default' }}>
            <div className="worker-icon">
                <Icon name="video_library" />
            </div>
            <div className="worker-info">
                <div className="worker-title">{title}</div>
                <div className="worker-status">{status}</div>
                {detail && <div className="worker-status">{detail}</div>}
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
