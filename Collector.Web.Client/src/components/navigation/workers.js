// Worker component mappings
// Maps worker routes to their corresponding display components

import VideoWorker from './workers/video-worker';
import ChatWorker from './workers/chat-worker';

const workers = {
    'video-worker': {
        component: VideoWorker,
        name: 'Video Download'
    },
    'chat-worker': {
        component: ChatWorker,
        name: 'AI Assistant'
    }
};

export default workers;
