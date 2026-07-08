// Worker component mappings
// Maps worker routes to their corresponding display components

import VideoWorker from './workers/video-worker';
import ChatWorker from './workers/chat-worker';
import DownloadWorker from './workers/download-worker';
import DomainWorker from './workers/domain-worker';

const workers = {
    'video-worker': {
        component: VideoWorker,
        name: 'Video Download'
    },
    'chat-worker': {
        component: ChatWorker,
        name: 'AI Assistant'
    },
    'download-worker': {
        component: DownloadWorker,
        name: 'Download Worker'
    },
    'domain-worker': {
        component: DomainWorker,
        name: 'Domain Analyzer'
    }
};

export default workers;
