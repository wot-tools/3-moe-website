class MarksController < ApplicationController

	def index
		#@marks = Mark.all
		@q = Mark.all.ransack(params[:q])
		@marks = @q.result.paginate(:page => params[:page], :per_page => 20)
	end

end
