import React, { useState } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Checkbox from '@/components/forms/checkbox';

export default function GenerateContentModal({ 
    initialUrl = '',
    initialIncludeVideo = true,
    initialIncludeTitle = true,
    initialIncludeDescription = true,
    initialIncludeTranscriptResearch = false,
    initialIncludeCommentsResearch = false,
    onGenerate,
    onClose 
}) {
    const [url, setUrl] = useState(initialUrl);
    const [includeVideo, setIncludeVideo] = useState(initialIncludeVideo);
    const [includeTitle, setIncludeTitle] = useState(initialIncludeTitle);
    const [includeDescription, setIncludeDescription] = useState(initialIncludeDescription);
    const [includeTranscriptResearch, setIncludeTranscriptResearch] = useState(initialIncludeTranscriptResearch);
    const [includeCommentsResearch, setIncludeCommentsResearch] = useState(initialIncludeCommentsResearch);

    const isYouTubeUrl = (urlString) => {
        if (!urlString) return false;
        const youtubePattern = /^(https?:\/\/)?(www\.|m\.)?(youtube\.com\/(watch\?v=|embed\/|v\/)|youtu\.be\/)[\w-]+/i;
        return youtubePattern.test(urlString);
    };

    const isYouTube = isYouTubeUrl(url);

    const handleGenerate = () => {
        if (onGenerate) {
            onGenerate({
                url,
                includeVideo,
                includeTitle,
                includeDescription,
                includeTranscriptResearch,
                includeCommentsResearch
            });
        }
        if (onClose) {
            onClose();
        }
    };

    return (
        <Modal title="Generate Content from URL" onClose={onClose}>
            <div className="modal-form">
                <div className="row">
                    <Input
                        label="URL"
                        name="url"
                        value={url}
                        onInput={(e) => setUrl(e.target.value)}
                        placeholder="https://youtube.com/watch?v=..."
                        autoFocus
                    />
                </div>

                {isYouTube && (
                    <>
                        <div className="row">
                            <h4 style={{ marginTop: '1em', marginBottom: '0.5em' }}>Content Options</h4>
                        </div>
                        <div className="row">
                            <Checkbox
                                label="Video"
                                name="include-video"
                                checked={includeVideo}
                                onChange={setIncludeVideo}
                            />
                        </div>
                        <div className="row">
                            <Checkbox
                                label="Title"
                                name="include-title"
                                checked={includeTitle}
                                onChange={setIncludeTitle}
                            />
                        </div>
                        <div className="row">
                            <Checkbox
                                label="Description"
                                name="include-description"
                                checked={includeDescription}
                                onChange={setIncludeDescription}
                            />
                        </div>
                        <div className="row">
                            <Checkbox
                                label="Transcript-Related Research"
                                name="include-transcript-research"
                                checked={includeTranscriptResearch}
                                onChange={setIncludeTranscriptResearch}
                            />
                        </div>
                        <div className="row">
                            <Checkbox
                                label="Comments-Based Research"
                                name="include-comments-research"
                                checked={includeCommentsResearch}
                                onChange={setIncludeCommentsResearch}
                            />
                        </div>
                    </>
                )}

                <div className="row">
                    <div className="buttons">
                        <button className="cancel" onClick={onClose}>Cancel</button>
                        <button
                            onClick={handleGenerate}
                            disabled={!url.trim() || !isYouTube}
                        >
                            Generate Content
                        </button>
                    </div>
                </div>
            </div>
        </Modal>
    );
}
