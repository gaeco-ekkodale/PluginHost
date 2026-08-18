// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React from 'react'

type ErrorCategory = 'not-found' | 'unavailable' | 'code' | 'unknown'

interface Props {
	children: React.ReactNode
	pluginName?: string
	externalError?: string | null
	externalErrorCategory?: ErrorCategory
}

interface State {
	hasError: boolean
	category: ErrorCategory
	errorMessage: string
}

const getCategory = (error: Error): ErrorCategory => {
	const text = `${error.name} ${error.message}`.toLowerCase()
	if (
		text.includes('404') ||
		text.includes('not found') ||
		text.includes('plugin_url_404')
	) {
		return 'not-found'
	}
	if (
		text.includes('failed to fetch') ||
		text.includes('network') ||
		text.includes('loading chunk') ||
		text.includes('importing a module script failed') ||
		text.includes('timeout') ||
		text.includes('plugin_url_unavailable')
	) {
		return 'unavailable'
	}
	if (
		text.includes('syntaxerror') ||
		text.includes('referenceerror') ||
		text.includes('typeerror')
	) {
		return 'code'
	}
	return 'unknown'
}

class ErrorBoundary extends React.Component<Props, State> {
	constructor(props: Props) {
		super(props)
		this.state = {
			hasError: false,
			category: 'unknown',
			errorMessage: '',
		}
	}

	static getDerivedStateFromError() {
		return { hasError: true }
	}

	componentDidCatch(error: Error) {
		console.error('Plugin error:', error)
		this.setState({
			category: getCategory(error),
			errorMessage: error.message || 'Unknown error',
		})
	}

	render() {
		if (this.state.hasError || this.props.externalError) {
			const name = this.props.pluginName || 'The module'
			const category = this.props.externalError
				? this.props.externalErrorCategory ||
					getCategory(new Error(this.props.externalError))
				: this.state.category
			const errorMessage =
				this.props.externalError || this.state.errorMessage

			return (
				<div className='flex min-h-[60vh] items-center justify-center p-6'>
					<div
						role='alert'
						className='w-full max-w-2xl rounded-2xl border border-red-200 bg-linear-to-b from-white to-red-50 p-6 shadow-xl ring-1 ring-red-100'
					>
						<div className='mb-4 flex items-start gap-3'>
							<div className='mt-0.5 rounded-full bg-red-100 p-2 text-red-700'>
								<span className='text-lg font-bold'>!</span>
							</div>
							<div>
								<h2 className='text-xl font-semibold text-red-900'>
									{name} could not be loaded
								</h2>
								<p className='mt-1 text-sm text-red-800/90'>
									{category === 'not-found'
										? 'The module URL was not found (404).'
										: category === 'unavailable'
											? 'The module is currently unreachable or temporarily unavailable.'
											: category === 'code'
												? 'A runtime or build error occurred inside the module.'
												: 'An unexpected error occurred while loading.'}
								</p>
							</div>
						</div>

						<div className='rounded-xl border border-red-100 bg-white/80 p-4'>
							<p className='mb-2 text-sm font-medium text-slate-700'>
								What you can check:
							</p>
							<ul className='list-disc space-y-1 pl-5 text-sm text-slate-700'>
								<li>
									Whether the module's container is running
									and its URL is reachable.
								</li>
								<li>
									For code errors: the module's build/deploy
									and the browser console.
								</li>
								<li>
									Reload the page and try again in a moment.
								</li>
							</ul>
						</div>

						{errorMessage && (
							<div className='mt-4 rounded-xl bg-slate-900 p-3 text-xs text-slate-100'>
								Technical detail: {errorMessage}
							</div>
						)}

						<div className='mt-5 flex justify-end'>
							<button
								type='button'
								onClick={() => window.location.reload()}
								className='rounded-lg bg-red-700 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-800'
							>
								Reload page
							</button>
						</div>
					</div>
				</div>
			)
		}
		return this.props.children
	}
}

export default ErrorBoundary
