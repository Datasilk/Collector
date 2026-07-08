import React from 'react';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import { printDate, localDateTime } from '@/helpers/datetime';
import './article-modal.css';

/**
 * <summary>Article Detail Modal</summary>
 * <description>Displays read-only details for an article.</description>
 */
export default function ArticleModal({ article, onClose, onDownload }) {
    if (!article) return null;

    const openUrl = () => {
        if (article.url) {
            window.open(article.url, '_blank', 'noopener,noreferrer');
        }
    };

    const handleDownload = () => {
        if (typeof onDownload === 'function') {
            onDownload(article);
        }
    };

    return (
        <Modal
            title="Article Details"
            onClose={onClose}
            wide={true}
        >
            <div className="article-detail">
                <div className="article-detail-row">
                    <label>Title</label>
                    <div>{article.title || 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>URL</label>
                    <div className="article-url">
                        {article.url ? (
                            <a href={article.url} target="_blank" rel="noopener noreferrer">
                                {article.url}
                            </a>
                        ) : 'N/A'}
                    </div>
                </div>
                <div className="article-detail-row">
                    <label>Domain</label>
                    <div>{article.domain || 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Subject</label>
                    <div>{article.subjectTitle || 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Published</label>
                    <div>{article.datepublished ? printDate(localDateTime(new Date(article.datepublished))) : 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Status</label>
                    <div>{article.active === true ? 'Active' : article.active === false ? 'Inactive' : 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Score</label>
                    <div>{article.score ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Words</label>
                    <div>{article.wordcount ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Sentences</label>
                    <div>{article.sentencecount ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Paragraphs</label>
                    <div>{article.paragraphcount ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Important Words</label>
                    <div>{article.importantcount ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Images</label>
                    <div>{article.images ?? 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>File Size</label>
                    <div>{article.filesize ? `${article.filesize} KB` : 'N/A'}</div>
                </div>
                <div className="article-detail-row">
                    <label>Visited</label>
                    <div>{article.visited ?? 0}</div>
                </div>
                <div className="article-detail-row full">
                    <label>Summary</label>
                    <div className="article-summary">{article.summary || 'No summary available'}</div>
                </div>
            </div>
            <div className="buttons">
                <button className="submit" onClick={openUrl}>
                    <Icon name="open_in_new" /> View Original
                </button>
                <button className="submit" onClick={handleDownload}>
                    <Icon name="download" /> Download
                </button>
                <button className="cancel" onClick={onClose}>Close</button>
            </div>
        </Modal>
    );
}
