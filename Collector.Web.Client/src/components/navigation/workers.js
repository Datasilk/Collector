// Worker component mappings
// Maps worker routes to their corresponding display components

import VideoWorker from './workers/video-worker';

const workers = {
    'video-worker': {
        component: VideoWorker,
        name: 'Video Download'
    }
};

export default workers;
