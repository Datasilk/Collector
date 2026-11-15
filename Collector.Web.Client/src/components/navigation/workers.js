// Worker component mappings
// Maps worker routes to their corresponding display components

import VideoWorker from './workers/video-worker';
import YoutubeChannelWorker from './workers/youtube-channel-worker';
import ChatWorker from './workers/chat-worker';
import DownloadWorker from './workers/download-worker';
import DomainWorker from './workers/domain-worker';

const workers = {
    'video-worker': {
        component: VideoWorker,
        name: 'Video Download'
    },
    'youtube-channel-worker': {
        component: YoutubeChannelWorker,
        name: 'YouTube Channel Import'
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
